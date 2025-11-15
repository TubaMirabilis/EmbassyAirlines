using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.TimeZones;
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
        var departureTime = LocalDateTime.FromDateTime(dto.DepartureLocalTime);
        if (!Enum.TryParse<SchedulingAmbiguityPolicy>(dto.SchedulingAmbiguityPolicy, out var schedulingAmbiguityPolicy))
        {
            _logger.LogWarning("Invalid scheduling ambiguity policy: {Policy}", dto.SchedulingAmbiguityPolicy);
            var error = Error.Validation("Flight.InvalidSchedulingAmbiguityPolicy", "Invalid scheduling ambiguity policy");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var resolver = ZoneLocalMappingResolver.FromSchedulingAmbiguityPolicy(schedulingAmbiguityPolicy);
        try
        {
            var departureInstant = departureTime.InZone(departureAirport.TimeZone, resolver).ToInstant();
            if (departureInstant < SystemClock.Instance.GetCurrentInstant())
            {
                _logger.LogWarning("Departure time cannot be in the past");
                var error = Error.Validation("Flight.DepartureTimeInPast", "Departure time cannot be in the past");
                return ErrorHandlingHelper.HandleProblem(error);
            }
            var arrivalTime = LocalDateTime.FromDateTime(dto.ArrivalLocalTime);
            var arrivalInstant = arrivalTime.InZone(arrivalAirport.TimeZone, resolver).ToInstant();
            if (arrivalInstant < departureInstant)
            {
                _logger.LogWarning("Arrival time cannot be before departure time");
                var error = Error.Validation("Flight.ArrivalTimeBeforeDeparture", "Arrival time cannot be before departure time");
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
            var args = new FlightCreationArgs
            {
                FlightNumberIata = dto.FlightNumberIata,
                FlightNumberIcao = dto.FlightNumberIcao,
                DepartureLocalTime = departureTime,
                ArrivalLocalTime = arrivalTime,
                DepartureAirport = departureAirport,
                ArrivalAirport = arrivalAirport,
                Aircraft = aircraft,
                EconomyPrice = economyPrice,
                BusinessPrice = businessPrice,
                SchedulingAmbiguityPolicy = schedulingAmbiguityPolicy
            };
            var flight = Flight.Create(args);
            ctx.Flights.Add(flight);
            await ctx.SaveChangesAsync(ct);
            return TypedResults.Created($"/flights/{flight.Id}", flight.ToDto());
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
