using AWS.Messaging;
using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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
    private static async Task<Results<Created<FlightDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
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
        if (!Enum.TryParse<OperationType>(dto.OperationType, out var operationType))
        {
            logger.LogWarning("Invalid operation type: {Type}", dto.OperationType);
            var error = Error.Validation("Flight.InvalidOperationType", $"Invalid operation type: {dto.OperationType}");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        if (!Enum.TryParse<SchedulingAmbiguityPolicy>(dto.SchedulingAmbiguityPolicy, out var schedulingAmbiguityPolicy))
        {
            logger.LogWarning("Invalid scheduling ambiguity policy: {Policy}", dto.SchedulingAmbiguityPolicy);
            var error = Error.Validation("Flight.InvalidSchedulingAmbiguityPolicy", $"Invalid scheduling ambiguity policy: {dto.SchedulingAmbiguityPolicy}");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var departureAirport = await ctx.Airports
                                        .Where(a => a.Id == dto.DepartureAirportId)
                                        .SingleOrDefaultAsync(ct);
        if (departureAirport is null)
        {
            logger.LogWarning("Departure airport with ID {Id} not found", dto.DepartureAirportId);
            var error = Error.NotFound("Flight.DepartureAirportNotFound", $"Departure airport with ID {dto.DepartureAirportId} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var arrivalAirport = await ctx.Airports
                                      .Where(a => a.Id == dto.ArrivalAirportId)
                                      .SingleOrDefaultAsync(ct);
        if (arrivalAirport is null)
        {
            logger.LogWarning("Arrival airport with ID {Id} not found", dto.ArrivalAirportId);
            var error = Error.NotFound("Flight.ArrivalAirportNotFound", $"Arrival airport with ID {dto.ArrivalAirportId} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var aircraft = await ctx.Aircraft
                                .Where(a => a.Id == dto.AircraftId)
                                .SingleOrDefaultAsync(ct);
        if (aircraft is null)
        {
            logger.LogWarning("Aircraft with ID {Id} not found", dto.AircraftId);
            var error = Error.NotFound("Flight.AircraftNotFound", $"Aircraft with ID {dto.AircraftId} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        try
        {
            var economyPrice = new Money(dto.EconomyPrice);
            var businessPrice = new Money(dto.BusinessPrice);
            var schedule = new FlightSchedule(new FlightScheduleCreationArgs
            {
                ArrivalAirport = arrivalAirport,
                ArrivalLocalTime = LocalDateTime.FromDateTime(dto.ArrivalLocalTime),
                DepartureAirport = departureAirport,
                DepartureLocalTime = LocalDateTime.FromDateTime(dto.DepartureLocalTime),
                Now = clock.GetCurrentInstant(),
                SchedulingAmbiguityPolicy = schedulingAmbiguityPolicy
            });
            var flight = Flight.Create(new FlightCreationArgs
            {
                Aircraft = aircraft,
                BusinessPrice = businessPrice,
                CreatedAt = clock.GetCurrentInstant(),
                EconomyPrice = economyPrice,
                FlightNumberIata = dto.FlightNumberIata,
                FlightNumberIcao = dto.FlightNumberIcao,
                Schedule = schedule,
                OperationType = operationType
            });
            ctx.Flights.Add(flight);
            await ctx.SaveChangesAsync(ct);
            logger.LogInformation(
                "Scheduled new flight {Id}: Departure - {DepartureLocalTime}, Arrival - {ArrivalLocalTime}",
                flight.Id,
                dto.DepartureLocalTime,
                dto.ArrivalLocalTime);
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
