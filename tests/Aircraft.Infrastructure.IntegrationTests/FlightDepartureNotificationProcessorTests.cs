using Aircraft.Core.Models;
using Shared.Contracts;

namespace Aircraft.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class FlightDepartureNotificationProcessorTests
{
    private static readonly DateTimeOffset CreatedAt = At(6, 0);

    private readonly PostgreSqlFixture _postgres;

    public FlightDepartureNotificationProcessorTests(PostgreSqlFixture postgres)
        => _postgres = postgres;

    [Fact]
    public async Task New_departure_should_mark_aircraft_en_route()
    {
        // Arrange
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", CreatedAt);
        var eventId = Guid.NewGuid();

        // Act
        var result = await CreateProcessor(At(10, 5)).ProcessAsync(
            eventId,
            aircraftId,
            "EHAM",
            At(10, 0),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(AircraftSituation.EnRoute("EHAM", At(10, 0)), aircraft.Situation);
        Assert.Equal(At(10, 5), aircraft.UpdatedAt);

        Assert.True(await _postgres.InboxContainsAsync(eventId));
    }

    [Fact]
    public async Task Retrospective_departure_should_apply_when_aircraft_was_modified_for_another_reason_after_departing()
    {
        // Arrange: the aircraft parks at EGLL at 10:00 and is modified for an unrelated
        // reason at 11:45; its 11:30 departure is not recorded until 12:00.
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", CreatedAt);
        await ProcessArrival(aircraftId, "EGLL", At(10, 0), At(10, 5));
        await _postgres.SetUpdatedAtAsync(aircraftId, At(11, 45));
        var eventId = Guid.NewGuid();

        // Act
        var result = await CreateProcessor(At(12, 0)).ProcessAsync(
            eventId,
            aircraftId,
            "KJFK",
            At(11, 30),
            TestContext.Current.CancellationToken);

        // Assert: the departure is ordered against the 10:00 arrival, not the 11:45 modification.
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(AircraftSituation.EnRoute("KJFK", At(11, 30)), aircraft.Situation);
        Assert.Equal(At(12, 0), aircraft.UpdatedAt);

        Assert.True(await _postgres.InboxContainsAsync(eventId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public async Task Departure_no_newer_than_last_flight_movement_should_be_stale(int minutesBeforeArrival)
    {
        // Arrange: the aircraft has already arrived when a departure that
        // took place no later than that arrival is delivered.
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", CreatedAt);
        await ProcessArrival(aircraftId, "EGLL", At(10, 0), At(10, 5));
        var eventId = Guid.NewGuid();

        // Act
        var result = await CreateProcessor(At(12, 0)).ProcessAsync(
            eventId,
            aircraftId,
            "KJFK",
            At(10, 0).AddMinutes(-minutesBeforeArrival),
            TestContext.Current.CancellationToken);

        // Assert: the aircraft is untouched, but the event is marked as processed.
        Assert.Equal(AircraftFlightStatusProcessingResult.Stale, result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(AircraftSituation.Parked("EGLL", At(10, 0)), aircraft.Situation);
        Assert.Equal(At(10, 5), aircraft.UpdatedAt);

        Assert.True(await _postgres.InboxContainsAsync(eventId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public async Task Departure_no_newer_than_aircraft_creation_should_be_stale(int minutesBeforeCreation)
    {
        // Arrange: an aircraft that has not moved is located as of its creation.
        await _postgres.ResetAsync();
        var aircraftId = await _postgres.CreateAircraftAsync("C-FJRN", CreatedAt);
        var eventId = Guid.NewGuid();

        // Act
        var result = await CreateProcessor(At(12, 0)).ProcessAsync(
            eventId,
            aircraftId,
            "KJFK",
            CreatedAt.AddMinutes(-minutesBeforeCreation),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AircraftFlightStatusProcessingResult.Stale, result);

        var aircraft = await _postgres.GetAircraftWithHistoryAsync(aircraftId);
        Assert.Equal(AircraftSituation.Parked("CYVR", CreatedAt), aircraft.Situation);
        Assert.Equal(CreatedAt, aircraft.UpdatedAt);

        Assert.True(await _postgres.InboxContainsAsync(eventId));
    }

    private static DateTimeOffset At(int hour, int minute) =>
        new(2026, 10, 16, hour, minute, 0, TimeSpan.Zero);

    private FlightDepartureNotificationProcessor CreateProcessor(DateTimeOffset utcNow) =>
        new(new TestDbContextFactory(_postgres), new FixedTimeProvider(utcNow));

    private async Task ProcessArrival(Guid aircraftId, string destination, DateTimeOffset onBlockAt, DateTimeOffset recordedAt)
    {
        var processor = new FlightArrivalNotificationProcessor(new TestDbContextFactory(_postgres), new FixedTimeProvider(recordedAt));
        var flightArrivedEvent = new FlightArrivedEvent(Guid.NewGuid(), aircraftId, Guid.NewGuid(), "CYVR", destination, onBlockAt.AddHours(-2), onBlockAt.AddHours(-2).AddMinutes(15), onBlockAt.AddMinutes(-10), onBlockAt);
        var result = await processor.ProcessAsync(flightArrivedEvent, TestContext.Current.CancellationToken);
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, result.AircraftStatus);
    }
}
