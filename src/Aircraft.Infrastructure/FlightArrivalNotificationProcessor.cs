using Aircraft.Core.Models;
using Aircraft.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure;

public sealed class FlightArrivalNotificationProcessor
{
    private const int MaxAttempts = 3;

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;

    public FlightArrivalNotificationProcessor(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<FlightArrivalProcessingResult> ProcessAsync(
        Guid eventId,
        Guid aircraftId,
        Guid flightId,
        string departureAirportIcao,
        string arrivalAirportIcao,
        FlightMilestones milestones,
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
                    return FlightArrivalProcessingResult.Duplicate;
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

                var now = _timeProvider.GetUtcNow();

                var flight = CompletedFlight.Create(
                    flightId,
                    departureAirportIcao,
                    arrivalAirportIcao,
                    milestones,
                    now);

                // History is recorded regardless of event ordering;
                // only the state transition below is subject to it.
                var history = await RecordCompletedFlightAsync(
                    ctx,
                    eventId,
                    aircraftId,
                    flight,
                    cancellationToken);

                if (aircraft.IsStaleFlightMovement(milestones.OnBlockAt))
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new FlightArrivalProcessingResult(
                        history,
                        AircraftFlightStatusProcessingResult.Stale);
                }

                aircraft.MarkAsParked(arrivalAirportIcao, milestones.OnBlockAt, now);

                await ctx.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new FlightArrivalProcessingResult(
                    history,
                    AircraftFlightStatusProcessingResult.Applied);
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

    private static async Task<CompletedFlightRecordingResult> RecordCompletedFlightAsync(
        ApplicationDbContext ctx,
        Guid eventId,
        Guid aircraftId,
        CompletedFlight flight,
        CancellationToken cancellationToken)
    {
        // If another transaction is inserting the same flight, ON CONFLICT
        // waits for it to finish, so a race between two events for one flight
        // resolves here rather than as a unique-constraint violation.
        var inserted = await ctx.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO aircraft.completed_flight
                (id, aircraft_id, departure_airport_icao, arrival_airport_icao,
                 milestones_off_block_at, milestones_take_off_at,
                 milestones_landing_at, milestones_on_block_at, created_at)
            VALUES
                ({flight.Id}, {aircraftId}, {flight.DepartureAirportIcao}, {flight.ArrivalAirportIcao},
                 {flight.Milestones.OffBlockAt}, {flight.Milestones.TakeOffAt},
                 {flight.Milestones.LandingAt}, {flight.Milestones.OnBlockAt}, {flight.CreatedAt})
            ON CONFLICT (id) DO NOTHING;
            """,
            cancellationToken);

        if (inserted == 1)
        {
            return CompletedFlightRecordingResult.Recorded;
        }

        // Compared in the database so that both sides of each timestamp
        // carry PostgreSQL's microsecond precision.
        var matchesExisting = await ctx.Set<CompletedFlight>()
            .AnyAsync(
                cf => cf.Id == flight.Id
                    && EF.Property<Guid?>(cf, "AircraftId") == aircraftId
                    && cf.DepartureAirportIcao == flight.DepartureAirportIcao
                    && cf.ArrivalAirportIcao == flight.ArrivalAirportIcao
                    && cf.Milestones.OffBlockAt == flight.Milestones.OffBlockAt
                    && cf.Milestones.TakeOffAt == flight.Milestones.TakeOffAt
                    && cf.Milestones.LandingAt == flight.Milestones.LandingAt
                    && cf.Milestones.OnBlockAt == flight.Milestones.OnBlockAt,
                cancellationToken);

        if (!matchesExisting)
        {
            throw new CompletedFlightConflictException(
                $"Event {eventId} reports flight {flight.Id} as completed by aircraft {aircraftId}, " +
                "but the flight's existing completed-flight record contains conflicting details. " +
                "The existing record has not been modified.");
        }

        return CompletedFlightRecordingResult.AlreadyRecorded;
    }
}
