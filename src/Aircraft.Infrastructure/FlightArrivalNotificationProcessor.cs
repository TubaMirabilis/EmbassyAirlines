using System.Globalization;
using System.Text;
using Aircraft.Core.Models;
using Aircraft.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace Aircraft.Infrastructure;

public sealed class FlightArrivalNotificationProcessor
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;

    public FlightArrivalNotificationProcessor(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
    }

    public Task<FlightArrivalProcessingResult> ProcessAsync(FlightArrivedEvent flightArrivedEvent, CancellationToken cancellationToken = default)
        => AircraftConcurrencyRetry.ExecuteAsync(async ct =>
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        await using var transaction = await ctx.Database.BeginTransactionAsync(ct);
        var claimed = await ctx.Database.ExecuteSqlInterpolatedAsync(
        $"""
            INSERT INTO aircraft.inbox_messages
                (event_id, processed_on_utc)
            VALUES
                ({flightArrivedEvent.Id}, now())
            ON CONFLICT (event_id) DO NOTHING;
        """, ct);
        if (claimed == 0)
        {
            return FlightArrivalProcessingResult.Duplicate;
        }
        var aircraft = await ctx.Aircraft.SingleOrDefaultAsync(a => a.Id == flightArrivedEvent.AircraftId, ct);
        if (aircraft is null)
        {
            throw new InvalidOperationException($"Aircraft with ID {flightArrivedEvent.AircraftId} was not found.");
        }
        var now = _timeProvider.GetUtcNow();
        var milestones = new FlightMilestones(flightArrivedEvent.OffBlockAt, flightArrivedEvent.TakeOffAt, flightArrivedEvent.LandingAt, flightArrivedEvent.OnBlockAt);
        var args = new CompletedFlightCreationArgs
        {
            ArrivalAirportIcao = flightArrivedEvent.ArrivalAirportIcaoCode,
            CreatedAt = now,
            DepartureAirportIcao = flightArrivedEvent.DepartureAirportIcaoCode,
            Id = flightArrivedEvent.FlightId,
            Milestones = milestones
        };
        var flight = CompletedFlight.Create(args);
        var history = await RecordCompletedFlightAsync(ctx, flightArrivedEvent.Id, flightArrivedEvent.AircraftId, flight, ct);
        if (aircraft.IsStaleFlightMovement(milestones.OnBlockAt))
        {
            await transaction.CommitAsync(ct);
            return new FlightArrivalProcessingResult(history, AircraftFlightStatusProcessingResult.Stale);
        }
        aircraft.MarkAsParked(flightArrivedEvent.ArrivalAirportIcaoCode, milestones.OnBlockAt, now);
        await ctx.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new FlightArrivalProcessingResult(history, AircraftFlightStatusProcessingResult.Applied);
    }, cancellationToken);

    private static async Task<CompletedFlightRecordingResult> RecordCompletedFlightAsync(ApplicationDbContext ctx,
                                                                                         Guid eventId,
                                                                                         Guid aircraftId,
                                                                                         CompletedFlight flight,
                                                                                         CancellationToken cancellationToken)
    {
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
            """, cancellationToken);
        if (inserted == 1)
        {
            return CompletedFlightRecordingResult.Recorded;
        }
        var specification = new MatchingCompletedFlightSpecification(aircraftId, flight);
        var matchesExisting = await ctx.Set<CompletedFlight>()
                                       .AnyAsync(specification.Criteria, cancellationToken);
        if (!matchesExisting)
        {
            var message = new StringBuilder();
            message.Append(CultureInfo.InvariantCulture, $"Event {eventId} reports flight {flight.Id} as completed by aircraft {aircraftId}. ");
            message.Append("The existing completed-flight record contains conflicting details. ");
            message.Append("The existing record has not been modified.");
            throw new CompletedFlightConflictException(message.ToString());
        }
        return CompletedFlightRecordingResult.AlreadyRecorded;
    }
}
