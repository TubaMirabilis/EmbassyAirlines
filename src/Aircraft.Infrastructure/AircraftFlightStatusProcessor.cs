using Aircraft.Core.Models;
using Aircraft.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure;

public sealed class AircraftFlightStatusProcessor
{
    private const int MaxAttempts = 3;

    private readonly ApplicationDbContext _ctx;

    public AircraftFlightStatusProcessor(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<AircraftFlightStatusProcessingResult> ProcessAsync(
        Guid aircraftId,
        Status status,
        string airportIcaoCode,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var aircraft = await _ctx.Aircraft.FindAsync(
                [aircraftId],
                cancellationToken);

            if (aircraft is null)
            {
                throw new InvalidOperationException(
                    $"Aircraft with ID {aircraftId} was not found.");
            }

            if (occurredAt <= aircraft.UpdatedAt)
            {
                return AircraftFlightStatusProcessingResult.Stale;
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
                    throw new ArgumentOutOfRangeException(
                        nameof(status), status, null);
            }

            try
            {
                await _ctx.SaveChangesAsync(cancellationToken);

                return AircraftFlightStatusProcessingResult.Applied;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                await _ctx.Entry(aircraft)
                    .ReloadAsync(cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Aircraft status processing exhausted its retry attempts.");
    }
}
