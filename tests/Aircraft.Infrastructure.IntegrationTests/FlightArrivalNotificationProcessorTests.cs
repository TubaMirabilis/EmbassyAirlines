using Aircraft.Core.Models;
using Aircraft.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Aircraft.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class FlightArrivalNotificationProcessorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly FlightMilestones ArrivalMilestones = new(
        T0.AddMinutes(30),
        T0.AddMinutes(45),
        T0.AddHours(9).AddMinutes(50),
        T0.AddHours(10));

    private static readonly DateTimeOffset RecordedAt = T0.AddHours(11);

    private readonly PostgreSqlFixture _postgres;

    public FlightArrivalNotificationProcessorTests(PostgreSqlFixture postgres)
        => _postgres = postgres;

    [Fact]
    public async Task New_arrival_should_record_completed_flight_and_park_aircraft()
    {
        // Arrange
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var eventId = Guid.NewGuid();
        var flightId = Guid.NewGuid();

        // Act
        var result = await CreateProcessor(RecordedAt).ProcessAsync(
            eventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            new FlightArrivalProcessingResult(
                CompletedFlightRecordingResult.Recorded,
                AircraftFlightStatusProcessingResult.Applied),
            result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("EHAM", aircraft.ParkedAt);
        Assert.Null(aircraft.EnRouteTo);
        Assert.Equal(ArrivalMilestones.OnBlockAt, aircraft.LastFlightMovementAt);
        Assert.Equal(RecordedAt, aircraft.UpdatedAt);
        AssertCompletedFlight(Assert.Single(aircraft.CompletedFlights), flightId, RecordedAt);

        Assert.True(await _postgres.InboxContainsAsync(eventId));
    }

    [Fact]
    public async Task Arrival_should_park_aircraft_when_it_was_modified_for_another_reason_after_arriving()
    {
        // Arrange: a modification unrelated to flight movements lands after
        // the flight arrived but before its arrival event is delivered.
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        await _postgres.SetUpdatedAtAsync(aircraftId, ArrivalMilestones.OnBlockAt.AddMinutes(30));
        var eventId = Guid.NewGuid();
        var flightId = Guid.NewGuid();

        // Act
        var result = await CreateProcessor(RecordedAt).ProcessAsync(
            eventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        // Assert: the arrival is ordered against the last flight movement, not UpdatedAt.
        Assert.Equal(
            new FlightArrivalProcessingResult(
                CompletedFlightRecordingResult.Recorded,
                AircraftFlightStatusProcessingResult.Applied),
            result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("EHAM", aircraft.ParkedAt);
        Assert.Null(aircraft.EnRouteTo);
        Assert.Equal(ArrivalMilestones.OnBlockAt, aircraft.LastFlightMovementAt);
        Assert.Equal(RecordedAt, aircraft.UpdatedAt);
    }

    [Fact]
    public async Task Late_arrival_should_record_completed_flight_without_changing_newer_aircraft_state()
    {
        // Arrange: the aircraft has already departed on its next flight
        // by the time the earlier flight's arrival is delivered.
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var departedAt = ArrivalMilestones.OnBlockAt.AddHours(2);
        var departureRecordedAt = departedAt.AddMinutes(5);
        await ProcessDeparture(aircraftId, "EGLL", departedAt, departureRecordedAt, TestContext.Current.CancellationToken);
        var eventId = Guid.NewGuid();
        var flightId = Guid.NewGuid();

        // Act
        var result = await CreateProcessor(RecordedAt).ProcessAsync(
            eventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        // Assert: history is recorded; status, location, destination and timestamps are not.
        Assert.Equal(
            new FlightArrivalProcessingResult(
                CompletedFlightRecordingResult.Recorded,
                AircraftFlightStatusProcessingResult.Stale),
            result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(Status.EnRoute, aircraft.Status);
        Assert.Equal("EGLL", aircraft.EnRouteTo);
        Assert.Null(aircraft.ParkedAt);
        Assert.Equal(departedAt, aircraft.LastFlightMovementAt);
        Assert.Equal(departureRecordedAt, aircraft.UpdatedAt);
        AssertCompletedFlight(Assert.Single(aircraft.CompletedFlights), flightId, RecordedAt);

        Assert.True(await _postgres.InboxContainsAsync(eventId));
    }

    [Fact]
    public async Task Redelivered_event_should_return_duplicate_without_touching_history_or_aircraft()
    {
        // Arrange
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var eventId = Guid.NewGuid();
        var flightId = Guid.NewGuid();

        await CreateProcessor(RecordedAt).ProcessAsync(
            eventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        // Act: simulate SQS redelivery of the same message.
        var result = await CreateProcessor(RecordedAt.AddHours(1)).ProcessAsync(
            eventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(FlightArrivalProcessingResult.Duplicate, result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("EHAM", aircraft.ParkedAt);
        Assert.Equal(ArrivalMilestones.OnBlockAt, aircraft.LastFlightMovementAt);
        Assert.Equal(RecordedAt, aircraft.UpdatedAt);
        AssertCompletedFlight(Assert.Single(aircraft.CompletedFlights), flightId, RecordedAt);
    }

    [Fact]
    public async Task Different_event_for_already_recorded_flight_should_not_record_it_again()
    {
        // Arrange
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var firstEventId = Guid.NewGuid();
        var secondEventId = Guid.NewGuid();
        var flightId = Guid.NewGuid();

        await CreateProcessor(RecordedAt).ProcessAsync(
            firstEventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        // Act
        var result = await CreateProcessor(RecordedAt.AddHours(1)).ProcessAsync(
            secondEventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        // Assert: the original record, including its creation time, is kept.
        Assert.Equal(
            new FlightArrivalProcessingResult(
                CompletedFlightRecordingResult.AlreadyRecorded,
                AircraftFlightStatusProcessingResult.Stale),
            result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(RecordedAt, aircraft.UpdatedAt);
        AssertCompletedFlight(Assert.Single(aircraft.CompletedFlights), flightId, RecordedAt);

        Assert.True(await _postgres.InboxContainsAsync(firstEventId));
        Assert.True(await _postgres.InboxContainsAsync(secondEventId));
    }

    [Theory]
    [InlineData("CYYZ", "EHAM", 0)]
    [InlineData("CYVR", "EGLL", 0)]
    [InlineData("CYVR", "EHAM", 60)]
    public async Task Conflicting_event_for_recorded_flight_should_throw_and_roll_back(
        string departureAirportIcao,
        string arrivalAirportIcao,
        int onBlockDelaySeconds)
    {
        // Arrange
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var flightId = Guid.NewGuid();

        await CreateProcessor(RecordedAt).ProcessAsync(
            Guid.NewGuid(),
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        var conflictingEventId = Guid.NewGuid();
        var conflictingMilestones = new FlightMilestones(
            ArrivalMilestones.OffBlockAt,
            ArrivalMilestones.TakeOffAt,
            ArrivalMilestones.LandingAt,
            ArrivalMilestones.OnBlockAt.AddSeconds(onBlockDelaySeconds));

        // Act
        await Assert.ThrowsAsync<CompletedFlightConflictException>(() =>
            CreateProcessor(RecordedAt.AddHours(1)).ProcessAsync(
                conflictingEventId,
                aircraftId,
                flightId,
                departureAirportIcao,
                arrivalAirportIcao,
                conflictingMilestones,
                TestContext.Current.CancellationToken));

        // Assert: neither history nor aircraft state changed, and the event
        // is not marked as processed, so it is retried and then dead-lettered.
        Assert.False(await _postgres.InboxContainsAsync(conflictingEventId));

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("EHAM", aircraft.ParkedAt);
        Assert.Equal(ArrivalMilestones.OnBlockAt, aircraft.LastFlightMovementAt);
        Assert.Equal(RecordedAt, aircraft.UpdatedAt);
        AssertCompletedFlight(Assert.Single(aircraft.CompletedFlights), flightId, RecordedAt);
    }

    [Fact]
    public async Task Event_attributing_recorded_flight_to_another_aircraft_should_throw_and_roll_back()
    {
        // Arrange
        await _postgres.ResetAsync();
        var firstAircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var secondAircraftId = await _postgres.CreateAircraftAsync("C-FRSA", T0);
        var flightId = Guid.NewGuid();

        await CreateProcessor(RecordedAt).ProcessAsync(
            Guid.NewGuid(),
            firstAircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        var conflictingEventId = Guid.NewGuid();

        // Act
        await Assert.ThrowsAsync<CompletedFlightConflictException>(() =>
            CreateProcessor(RecordedAt.AddHours(1)).ProcessAsync(
                conflictingEventId,
                secondAircraftId,
                flightId,
                "CYVR",
                "EHAM",
                ArrivalMilestones,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.False(await _postgres.InboxContainsAsync(conflictingEventId));

        var firstAircraft = await _postgres.GetAircraftWithHistoryAsync(firstAircraftId);
        AssertCompletedFlight(Assert.Single(firstAircraft.CompletedFlights), flightId, RecordedAt);

        var secondAircraft = await _postgres.GetAircraftWithHistoryAsync(secondAircraftId);
        Assert.Equal(Status.Parked, secondAircraft.Status);
        Assert.Equal("CYVR", secondAircraft.ParkedAt);
        Assert.Null(secondAircraft.LastFlightMovementAt);
        Assert.Equal(T0, secondAircraft.UpdatedAt);
        Assert.Empty(secondAircraft.CompletedFlights);
    }

    [Fact]
    public async Task Event_for_flight_being_recorded_concurrently_should_wait_and_treat_it_as_already_recorded()
    {
        // Arrange: another transaction has inserted the same flight but not yet committed.
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var eventId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var concurrentlyRecordedAt = RecordedAt.AddMinutes(-1);

        await using var concurrentDb = _postgres.CreateDbContext();
        await using var concurrentTransaction =
            await concurrentDb.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await InsertCompletedFlight(concurrentDb, aircraftId, flightId, concurrentlyRecordedAt);

        // Act
        var processing = CreateProcessor(RecordedAt).ProcessAsync(
            eventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        await WaitUntilBlockedOnAnotherTransaction(processing);
        Assert.False(processing.IsCompleted);
        await concurrentTransaction.CommitAsync(TestContext.Current.CancellationToken);
        var result = await processing;

        // Assert: no unique-constraint violation surfaces, and the state transition still applies.
        Assert.Equal(
            new FlightArrivalProcessingResult(
                CompletedFlightRecordingResult.AlreadyRecorded,
                AircraftFlightStatusProcessingResult.Applied),
            result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("EHAM", aircraft.ParkedAt);
        Assert.Equal(ArrivalMilestones.OnBlockAt, aircraft.LastFlightMovementAt);
        Assert.Equal(RecordedAt, aircraft.UpdatedAt);
        AssertCompletedFlight(Assert.Single(aircraft.CompletedFlights), flightId, concurrentlyRecordedAt);

        Assert.True(await _postgres.InboxContainsAsync(eventId));
    }

    [Fact]
    public async Task Concurrency_conflict_should_retry_and_record_flight_when_concurrent_departure_makes_arrival_stale()
    {
        // Arrange: a departure on the aircraft's next flight commits between
        // the processor loading the aircraft and saving it.
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var eventId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var departedAt = ArrivalMilestones.OnBlockAt.AddHours(2);
        var departureRecordedAt = departedAt.AddMinutes(5);
        var conflicts = new ConcurrencyConflictCounter();
        var concurrentDeparture = new BeforeSaveChangesHook(
            1,
            ct => ProcessDeparture(aircraftId, "EGLL", departedAt, departureRecordedAt, ct));

        // Act
        var result = await CreateProcessor(RecordedAt, concurrentDeparture, conflicts).ProcessAsync(
            eventId,
            aircraftId,
            flightId,
            "CYVR",
            "EHAM",
            ArrivalMilestones,
            TestContext.Current.CancellationToken);

        // Assert: the retry re-recorded the history it rolled back, then found the arrival stale.
        Assert.Equal(1, conflicts.Count);
        Assert.Equal(
            new FlightArrivalProcessingResult(
                CompletedFlightRecordingResult.Recorded,
                AircraftFlightStatusProcessingResult.Stale),
            result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(Status.EnRoute, aircraft.Status);
        Assert.Equal("EGLL", aircraft.EnRouteTo);
        Assert.Null(aircraft.ParkedAt);
        Assert.Equal(departedAt, aircraft.LastFlightMovementAt);
        Assert.Equal(departureRecordedAt, aircraft.UpdatedAt);
        AssertCompletedFlight(Assert.Single(aircraft.CompletedFlights), flightId, RecordedAt);

        Assert.True(await _postgres.InboxContainsAsync(eventId));
    }

    [Fact]
    public async Task Failure_after_recording_flight_should_roll_back_inbox_claim_history_and_state()
    {
        // Arrange: every attempt loses the race to a competing write, so the processor gives up.
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", T0);
        var eventId = Guid.NewGuid();
        var conflicts = new ConcurrencyConflictCounter();
        var competingWrite = new BeforeSaveChangesHook(
            3,
            ct => TouchAircraft(aircraftId, ct));

        // Act
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            CreateProcessor(RecordedAt, competingWrite, conflicts).ProcessAsync(
                eventId,
                aircraftId,
                Guid.NewGuid(),
                "CYVR",
                "EHAM",
                ArrivalMilestones,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(3, conflicts.Count);
        Assert.False(await _postgres.InboxContainsAsync(eventId));

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("CYVR", aircraft.ParkedAt);
        Assert.Null(aircraft.LastFlightMovementAt);
        Assert.Equal(T0, aircraft.UpdatedAt);
        Assert.Empty(aircraft.CompletedFlights);
    }

    [Fact]
    public async Task Arrival_for_unknown_aircraft_should_throw_without_committing_inbox_claim()
    {
        // Arrange
        await _postgres.ResetAsync();
        var eventId = Guid.NewGuid();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateProcessor(RecordedAt).ProcessAsync(
                eventId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "CYVR",
                "EHAM",
                ArrivalMilestones,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.False(await _postgres.InboxContainsAsync(eventId));

        await using var db = _postgres.CreateDbContext();
        Assert.Equal(0, await db.Set<CompletedFlight>().CountAsync(TestContext.Current.CancellationToken));
    }

    private static void AssertCompletedFlight(CompletedFlight flight, Guid flightId, DateTimeOffset createdAt)
    {
        Assert.Equal(flightId, flight.Id);
        Assert.Equal("CYVR", flight.DepartureAirportIcao);
        Assert.Equal("EHAM", flight.ArrivalAirportIcao);
        Assert.Equal(ArrivalMilestones, flight.Milestones);
        Assert.Equal(createdAt, flight.CreatedAt);
    }

    private FlightArrivalNotificationProcessor CreateProcessor(
        DateTimeOffset utcNow,
        params IInterceptor[] interceptors) =>
        new(new TestDbContextFactory(_postgres, interceptors), new FixedTimeProvider(utcNow));

    private async Task ProcessDeparture(
        Guid aircraftId,
        string destination,
        DateTimeOffset departedAt,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        var processor = new FlightDepartureNotificationProcessor(
            new TestDbContextFactory(_postgres),
            new FixedTimeProvider(recordedAt));

        var result = await processor.ProcessAsync(
            Guid.NewGuid(),
            aircraftId,
            destination,
            departedAt,
            cancellationToken);

        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, result);
    }

    private async Task TouchAircraft(Guid aircraftId, CancellationToken cancellationToken)
    {
        // Any UPDATE writes a new row version, which changes xmin.
        await using var db = _postgres.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE aircraft.aircraft SET updated_at = updated_at WHERE id = {aircraftId};",
            cancellationToken);
    }

    private static async Task InsertCompletedFlight(
        ApplicationDbContext db,
        Guid aircraftId,
        Guid flightId,
        DateTimeOffset createdAt) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO aircraft.completed_flight
                (id, aircraft_id, departure_airport_icao, arrival_airport_icao,
                 milestones_off_block_at, milestones_take_off_at,
                 milestones_landing_at, milestones_on_block_at, created_at)
            VALUES
                ({flightId}, {aircraftId}, 'CYVR', 'EHAM',
                 {ArrivalMilestones.OffBlockAt}, {ArrivalMilestones.TakeOffAt},
                 {ArrivalMilestones.LandingAt}, {ArrivalMilestones.OnBlockAt}, {createdAt});
            """,
            TestContext.Current.CancellationToken);

    private async Task WaitUntilBlockedOnAnotherTransaction(Task processing)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync(timeout.Token);
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_locks WHERE locktype = 'transactionid' AND NOT granted);",
            connection);

        while (!processing.IsCompleted
            && !(bool)(await command.ExecuteScalarAsync(timeout.Token))!)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }
}
