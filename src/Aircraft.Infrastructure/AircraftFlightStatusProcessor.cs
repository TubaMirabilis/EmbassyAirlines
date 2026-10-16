using Aircraft.Core.Models;
using Aircraft.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure;

public sealed class AircraftFlightStatusProcessor
{
    private const int MaxAttempts = 3;

    private readonly ApplicationDbContext _ctx;

    public AircraftFlightStatusProcessor(ApplicationDbContext ctx)
        => _ctx = ctx;

    public async Task<AircraftFlightStatusProcessingResult> ProcessAsync(
        Guid eventId,
        Guid aircraftId,
        Status status,
        string airportIcaoCode,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await using var transaction =
                await _ctx.Database.BeginTransactionAsync(cancellationToken);

            var claimed = await _ctx.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO aircraft.inbox_messages
                    (event_id, processed_on_utc)
                VALUES
                    ({eventId}, now())
                ON CONFLICT (event_id) DO NOTHING;
                """,
                cancellationToken);

            if (claimed == 0)
            {
                // The original event has already been handled.
                // No aircraft update is necessary.
                return AircraftFlightStatusProcessingResult.Duplicate;
            }

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
                // Stale events are successfully handled by discarding them.
                // Commit their inbox records to prevent repeated work.
                await transaction.CommitAsync(cancellationToken);

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

                // The inbox claim and aircraft transition become
                // durable together.
                await transaction.CommitAsync(cancellationToken);

                return AircraftFlightStatusProcessingResult.Applied;
            }
            catch (DbUpdateConcurrencyException)
                when (attempt < MaxAttempts)
            {
                // The inbox claim must be rolled back too.
                await transaction.RollbackAsync(cancellationToken);

                // Refresh the entity and its xmin concurrency token.
                await _ctx.Entry(aircraft)
                    .ReloadAsync(cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Aircraft status processing exhausted its retry attempts.");
    }
}
