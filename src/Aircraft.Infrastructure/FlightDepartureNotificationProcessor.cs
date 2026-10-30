using Aircraft.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure;

public sealed class FlightDepartureNotificationProcessor
{
    private const int MaxAttempts = 3;

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;

    public FlightDepartureNotificationProcessor(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<AircraftFlightStatusProcessingResult> ProcessAsync(
        Guid eventId,
        Guid aircraftId,
        string airportIcaoCode,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await using var ctx =
                await _contextFactory.CreateDbContextAsync(cancellationToken);

            try
            {
                await using var transaction =
                    await ctx.Database.BeginTransactionAsync(cancellationToken);

                var claimed = await ctx.Database.ExecuteSqlInterpolatedAsync(
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
                    return AircraftFlightStatusProcessingResult.Duplicate;
                }

                var aircraft = await ctx.Aircraft
                    .SingleOrDefaultAsync(
                        a => a.Id == aircraftId,
                        cancellationToken);

                if (aircraft is null)
                {
                    throw new InvalidOperationException(
                        $"Aircraft with ID {aircraftId} was not found.");
                }

                if (aircraft.IsStaleFlightMovement(occurredAt))
                {
                    await transaction.CommitAsync(cancellationToken);
                    return AircraftFlightStatusProcessingResult.Stale;
                }

                aircraft.MarkAsEnRoute(airportIcaoCode, occurredAt, _timeProvider.GetUtcNow());

                await ctx.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return AircraftFlightStatusProcessingResult.Applied;
            }
            catch (DbUpdateConcurrencyException)
                when (attempt < MaxAttempts)
            {
                // The transaction and context are disposed
                // before the next iteration.
            }
        }

        throw new InvalidOperationException(
            "Aircraft status processing exhausted its retry attempts.");
    }
}
