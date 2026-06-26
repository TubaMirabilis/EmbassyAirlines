using ErrorOr;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Flights.Api.Lambda;

internal sealed class FlightScheduler
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<FlightScheduler> _logger;
    public FlightScheduler(ApplicationDbContext ctx, ILogger<FlightScheduler> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }
    public async Task<ErrorOr<FlightSchedulerDependencies>> LoadDependenciesAsync(Guid aircraftId,
                                                                                  Guid arrivalAirportId,
                                                                                  Guid departureAirportId,
                                                                                  CancellationToken ct)
    {
        var errors = new List<Error>();
        var aircraft = await _ctx.Aircraft
                                 .Where(a => a.Id == aircraftId)
                                 .SingleOrDefaultAsync(ct);
        if (aircraft is null)
        {
            _logger.LogWarning("Aircraft with ID {Id} not found", aircraftId);
            errors.Add(Error.NotFound("Flight.AircraftNotFound", $"Aircraft with ID {aircraftId} not found"));
        }
        var arrivalAirport = await _ctx.Airports
                                       .Where(a => a.Id == arrivalAirportId)
                                       .SingleOrDefaultAsync(ct);
        if (arrivalAirport is null)
        {
            _logger.LogWarning("Arrival airport with ID {Id} not found", arrivalAirportId);
            errors.Add(Error.NotFound("Flight.ArrivalAirportNotFound", $"Arrival airport with ID {arrivalAirportId} not found"));
        }
        var departureAirport = await _ctx.Airports
                                         .Where(a => a.Id == departureAirportId)
                                         .SingleOrDefaultAsync(ct);
        if (departureAirport is null)
        {
            _logger.LogWarning("Departure airport with ID {Id} not found", departureAirportId);
            errors.Add(Error.NotFound("Flight.DepartureAirportNotFound", $"Departure airport with ID {departureAirportId} not found"));
        }
        if (errors.Count > 0)
        {
            var message = string.Join("\n", errors.Select(e => e.Description));
            return Error.NotFound("Flight.DependencyLoadFailed", message);
        }
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(arrivalAirport);
        ArgumentNullException.ThrowIfNull(departureAirport);
        return new FlightSchedulerDependencies(aircraft, arrivalAirport, departureAirport);
    }
    public async Task ScheduleFlightAsync(Flight flight, CancellationToken ct)
    {
        _ctx.Flights.Add(flight);
        await _ctx.SaveChangesAsync(ct);
    }
}
