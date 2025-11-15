using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.TimeZones;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class RescheduleFlightEndpoint : IEndpoint
{
    private readonly IBus _bus;
    private readonly ILogger<RescheduleFlightEndpoint> _logger;
    public RescheduleFlightEndpoint(IBus bus, ILogger<RescheduleFlightEndpoint> logger)
    {
        _bus = bus;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/schedule", InvokeAsync);
    private async Task<IResult> InvokeAsync(ApplicationDbContext ctx, Guid id, RescheduleFlightDto dto, CancellationToken ct)
    {
        var flight = await ctx.Flights
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            _logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
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
            var departureInstant = departureTime.InZone(flight.DepartureAirport.TimeZone, resolver).ToInstant();
            if (departureInstant < SystemClock.Instance.GetCurrentInstant())
            {
                _logger.LogWarning("Departure time cannot be in the past");
                var error = Error.Validation("Flight.DepartureTimeInPast", "Departure time cannot be in the past");
                return ErrorHandlingHelper.HandleProblem(error);
            }
            var arrivalTime = LocalDateTime.FromDateTime(dto.ArrivalLocalTime);
            var arrivalInstant = arrivalTime.InZone(flight.ArrivalAirport.TimeZone, resolver).ToInstant();
            if (arrivalInstant < departureInstant)
            {
                _logger.LogWarning("Arrival time cannot be before departure time");
                var error = Error.Validation("Flight.ArrivalTimeBeforeDeparture", "Arrival time cannot be before departure time");
                return ErrorHandlingHelper.HandleProblem(error);
            }
            flight.Reschedule(departureTime, arrivalTime, schedulingAmbiguityPolicy);
            await ctx.SaveChangesAsync(ct);
            await _bus.Publish(new FlightRescheduledEvent(id, dto.DepartureLocalTime, dto.ArrivalLocalTime), ct);
            return TypedResults.Ok(flight.ToDto());
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
