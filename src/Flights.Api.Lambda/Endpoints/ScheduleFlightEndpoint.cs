using System.Globalization;
using System.Text;
using AWS.Messaging;
using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using NodaTime;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class ScheduleFlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("flights", InvokeAsync)
              .WithSummary("Schedule a new flight")
              .Accepts<ScheduleFlightDto>("application/json")
              .Produces<FlightDto>(StatusCodes.Status201Created)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Created<FlightDto>, ProblemHttpResult>> InvokeAsync(FlightScheduler flightScheduler,
                                                                                          IClock clock,
                                                                                          ILogger<ScheduleFlightEndpoint> logger,
                                                                                          IMessagePublisher publisher,
                                                                                          IValidator<ScheduleFlightDto> validator,
                                                                                          ScheduleFlightDto dto,
                                                                                          CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for flight creation: {Errors}", formattedErrors);
            var error = Error.Validation("Flight.ValidationFailed", formattedErrors);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var dependenciesResult = await flightScheduler.LoadDependenciesAsync(dto.AircraftId, dto.ArrivalAirportId, dto.DepartureAirportId, ct);
        if (dependenciesResult.IsError)
        {
            var error = dependenciesResult.FirstError;
            logger.LogWarning("Failed to load dependencies for flight scheduling: {Message}", error.Description);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var deps = dependenciesResult.Value;
        try
        {
            var economyPrice = new Money(dto.EconomyPrice);
            var businessPrice = new Money(dto.BusinessPrice);
            var operationType = Enum.Parse<OperationType>(dto.OperationType, ignoreCase: true);
            var schedulingAmbiguityPolicy = Enum.Parse<SchedulingAmbiguityPolicy>(dto.SchedulingAmbiguityPolicy, ignoreCase: true);
            var schedule = new FlightSchedule(new FlightScheduleCreationArgs
            {
                ArrivalAirport = deps.ArrivalAirport,
                ArrivalLocalTime = LocalDateTime.FromDateTime(dto.ArrivalLocalTime),
                DepartureAirport = deps.DepartureAirport,
                DepartureLocalTime = LocalDateTime.FromDateTime(dto.DepartureLocalTime),
                Now = clock.GetCurrentInstant(),
                SchedulingAmbiguityPolicy = schedulingAmbiguityPolicy
            });
            var flight = Flight.Create(new FlightCreationArgs
            {
                Aircraft = deps.Aircraft,
                BusinessPrice = businessPrice,
                CreatedAt = clock.GetCurrentInstant(),
                EconomyPrice = economyPrice,
                FlightNumberIata = dto.FlightNumberIata,
                FlightNumberIcao = dto.FlightNumberIcao,
                Schedule = schedule,
                OperationType = operationType
            });
            await flightScheduler.ScheduleFlightAsync(flight, ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                var details = new StringBuilder();
                details.AppendLine(CultureInfo.InvariantCulture, $"Departure time: {dto.DepartureLocalTime}.");
                details.AppendLine(CultureInfo.InvariantCulture, $"Arrival time: {dto.ArrivalLocalTime}.");
                logger.LogInformation("Scheduled new flight {Id}. {Details}", flight.Id, details.ToString());
            }
            var evnt = new FlightScheduledEvent(flight.Id, flight.OperationType.ToString(), flight.BusinessPrice.Amount, flight.EconomyPrice.Amount);
            await publisher.PublishAsync(evnt, ct);
            return TypedResults.Created($"/flights/{flight.Id}", flight.ToDto());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(ex, "Invalid operation while scheduling flight: {Message}", ex.Message);
            var error = Error.Validation("Flight.SchedulingFailed", ex.Message);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
    }
}
