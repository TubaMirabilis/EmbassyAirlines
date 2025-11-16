using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Endpoints;
using Shared.Extensions;

namespace Flights.Api.Endpoints;

internal sealed class ScheduleFlightEndpoint : IEndpoint
{
    private readonly IValidator<CreateOrUpdateFlightDto> _validator;
    private readonly ILogger<ScheduleFlightEndpoint> _logger;
    public ScheduleFlightEndpoint(IValidator<CreateOrUpdateFlightDto> validator, ILogger<ScheduleFlightEndpoint> logger)
    {
        _validator = validator;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("flights", InvokeAsync);
    private async Task<IResult> InvokeAsync(ApplicationDbContext ctx, CreateOrUpdateFlightDto dto, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            _logger.LogWarning("Validation failed for flight creation: {Errors}", formattedErrors);
            var error = Error.Validation("Flight.ValidationFailed", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var departureAirport = await ctx.Airports
                                        .Where(a => a.Id == dto.DepartureAirportId)
                                        .SingleOrDefaultAsync(ct);
        if (departureAirport is null)
        {
            _logger.LogWarning("Departure airport with ID {Id} not found", dto.DepartureAirportId);
            var error = Error.NotFound("Flight.DepartureAirportNotFound", "Departure airport not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var arrivalAirport = await ctx.Airports
                                      .Where(a => a.Id == dto.ArrivalAirportId)
                                      .SingleOrDefaultAsync(ct);
        if (arrivalAirport is null)
        {
            _logger.LogWarning("Arrival airport with ID {Id} not found", dto.ArrivalAirportId);
            var error = Error.NotFound("Flight.ArrivalAirportNotFound", "Arrival airport not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        if (!Enum.TryParse<SchedulingAmbiguityPolicy>(dto.SchedulingAmbiguityPolicy, out var schedulingAmbiguityPolicy))
        {
            _logger.LogWarning("Invalid scheduling ambiguity policy: {Policy}", dto.SchedulingAmbiguityPolicy);
            var error = Error.Validation("Flight.InvalidSchedulingAmbiguityPolicy", "Invalid scheduling ambiguity policy");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var aircraft = await ctx.Aircraft
                                .Where(a => a.Id == dto.AircraftId)
                                .SingleOrDefaultAsync(ct);
        if (aircraft is null)
        {
            _logger.LogWarning("Aircraft with ID {Id} not found", dto.AircraftId);
            var error = Error.NotFound("Flight.AircraftNotFound", $"Aircraft with ID {dto.AircraftId} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var economyPrice = new Money(dto.EconomyPrice);
        var businessPrice = new Money(dto.BusinessPrice);
        try
        {
            var schedule = new FlightSchedule(new FlightScheduleCreationArgs
            {
                DepartureAirport = departureAirport,
                DepartureLocalTime = dto.DepartureLocalTime,
                ArrivalAirport = arrivalAirport,
                ArrivalLocalTime = dto.ArrivalLocalTime,
                SchedulingAmbiguityPolicy = schedulingAmbiguityPolicy
            });
            var flight = Flight.Create(new FlightCreationArgs
            {
                Aircraft = aircraft,
                BusinessPrice = businessPrice,
                EconomyPrice = economyPrice,
                FlightNumberIata = dto.FlightNumberIata,
                FlightNumberIcao = dto.FlightNumberIcao,
                Schedule = schedule
            });
            ctx.Flights.Add(flight);
            await ctx.SaveChangesAsync(ct);
            return TypedResults.Created($"/flights/{flight.Id}", flight.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while scheduling flight: {Message}", ex.Message);
            var error = Error.Validation("Flight.SchedulingFailed", ex.Message);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        catch (SkippedTimeException ex)
        {
            _logger.LogWarning(ex, "Departure or arrival time falls within a skipped time period due to daylight saving time transition");
            var description = "Departure or arrival time falls within a skipped time period due to daylight saving time transition";
            var error = Error.Validation("Flight.SkippedTime", description);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        catch (AmbiguousTimeException ex)
        {
            _logger.LogWarning(ex, "Departure or arrival time is ambiguous due to daylight saving time transition");
            var description = "Departure or arrival time is ambiguous due to daylight saving time transition";
            var error = Error.Validation("Flight.AmbiguousTime", description);
            return ErrorHandlingHelper.HandleProblem(error);
        }
    }
}
