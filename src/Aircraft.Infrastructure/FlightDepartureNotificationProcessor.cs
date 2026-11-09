using Aircraft.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure;

public sealed class FlightDepartureNotificationProcessor
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;

    public FlightDepartureNotificationProcessor(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
    }
    public Task<AircraftFlightStatusProcessingResult> ProcessAsync(
        Guid eventId,
        Guid aircraftId,
        string airportIcaoCode,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default) => AircraftConcurrencyRetry.ExecuteAsync(async ct =>
    {
        await using var ctx =
            await _contextFactory.CreateDbContextAsync(ct);

        await using var transaction =
            await ctx.Database.BeginTransactionAsync(ct);

        var claimed = await ctx.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO aircraft.inbox_messages
                (event_id, processed_on_utc)
            VALUES
                ({eventId}, now())
            ON CONFLICT (event_id) DO NOTHING;
            """,
            ct);

        if (claimed == 0)
        {
            return AircraftFlightStatusProcessingResult.Duplicate;
        }

        var aircraft = await ctx.Aircraft
            .SingleOrDefaultAsync(
                a => a.Id == aircraftId,
                ct);

        if (aircraft is null)
        {
            throw new InvalidOperationException(
                $"Aircraft with ID {aircraftId} was not found.");
        }

        if (aircraft.IsStaleFlightMovement(occurredAt))
        {
            await transaction.CommitAsync(ct);
            return AircraftFlightStatusProcessingResult.Stale;
        }

        aircraft.MarkAsEnRoute(
            airportIcaoCode,
            occurredAt,
            _timeProvider.GetUtcNow());

        await ctx.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return AircraftFlightStatusProcessingResult.Applied;
    }, cancellationToken);
}
