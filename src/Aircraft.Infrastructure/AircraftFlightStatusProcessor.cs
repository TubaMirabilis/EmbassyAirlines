using Aircraft.Core.Models;
using Aircraft.Infrastructure.Database;

namespace Aircraft.Infrastructure;

public sealed class AircraftFlightStatusProcessor
{
    private readonly ApplicationDbContext _ctx;
    public AircraftFlightStatusProcessor(ApplicationDbContext ctx) => _ctx = ctx;
    public async Task ProcessAsync(Guid aircraftId, Status status, string airportIcaoCode, DateTimeOffset occurredAt)
    {
        var aircraft = await _ctx.Aircraft.FindAsync(aircraftId);
        if (aircraft is null)
        {
            throw new InvalidOperationException($"Aircraft with ID {aircraftId} was not found.");
        }
        switch (status)
        {
            case Status.Parked:
                aircraft.MarkAsParked(airportIcaoCode, occurredAt);
                break;
            case Status.EnRoute:
                aircraft.MarkAsEnRoute(airportIcaoCode, occurredAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
        await _ctx.SaveChangesAsync();
    }
}
