using ErrorOr;
using Flights.Api.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class RescheduleFlightEndpoint : IEndpoint
{
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<RescheduleFlightEndpoint> _logger;
    public RescheduleFlightEndpoint(IBus bus, IServiceScopeFactory factory, ILogger<RescheduleFlightEndpoint> logger)
    {
        _bus = bus;
        _factory = factory;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/schedule", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, RescheduleFlightDto dto, CancellationToken ct)
    {
        using var scope = _factory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var flight = await ctx.Flights
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            _logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var departureTime = LocalDateTime.FromDateTime(dto.DepartureLocalTime);
        var departureInstant = departureTime.InZoneStrictly(flight.DepartureAirport.TimeZone).ToInstant();
        if (departureInstant < SystemClock.Instance.GetCurrentInstant())
        {
            _logger.LogWarning("Departure time cannot be in the past");
            var error = Error.Validation("Flight.DepartureTimeInPast", "Departure time cannot be in the past");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var arrivalTime = LocalDateTime.FromDateTime(dto.ArrivalLocalTime);
        var arrivalInstant = arrivalTime.InZoneStrictly(flight.ArrivalAirport.TimeZone).ToInstant();
        if (arrivalInstant < departureInstant)
        {
            _logger.LogWarning("Arrival time cannot be before departure time");
            var error = Error.Validation("Flight.ArrivalTimeBeforeDeparture", "Arrival time cannot be before departure time");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        flight.Reschedule(departureTime, arrivalTime);
        await ctx.SaveChangesAsync(ct);
        await _bus.Publish(new FlightRescheduledEvent(id, dto.DepartureLocalTime, dto.ArrivalLocalTime), ct);
        return TypedResults.Ok(flight.ToDto());
    }
}
