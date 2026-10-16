using Aircraft.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class AircraftFlightStatusProcessorTests
{
    private readonly PostgreSqlFixture _postgres;

    public AircraftFlightStatusProcessorTests(PostgreSqlFixture postgres)
        => _postgres = postgres;

    [Fact]
    public async Task Concurrent_aircraft_updates_should_discard_older_event_after_conflict()
    {
        // Arrange
        await ResetDatabase();

        var t0 = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        var aircraftId = await CreateAircraft(t0);

        var eventIdA = Guid.NewGuid();
        var eventIdB = Guid.NewGuid();

        var conflictsA = new ConcurrencyConflictCounter();
        var conflictsB = new ConcurrencyConflictCounter();

        await using var dbA = _postgres.CreateDbContext(conflictsA);
        await using var dbB = _postgres.CreateDbContext(conflictsB);

        // Load both entities before either processor saves.
        // Context B retains the original tracked entity and xmin.
        await dbA.Aircraft.SingleAsync(
            x => x.Id == aircraftId,
            TestContext.Current.CancellationToken);

        await dbB.Aircraft.SingleAsync(
            x => x.Id == aircraftId,
            TestContext.Current.CancellationToken);

        var processorA = new AircraftFlightStatusProcessor(dbA);
        var processorB = new AircraftFlightStatusProcessor(dbB);

        // Act
        var resultA = await processorA.ProcessAsync(
            eventIdA,
            aircraftId,
            Status.EnRoute,
            "EHAM",
            t0.AddMinutes(10),
            TestContext.Current.CancellationToken);

        var resultB = await processorB.ProcessAsync(
            eventIdB,
            aircraftId,
            Status.Parked,
            "RKSI",
            t0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, resultA);
        Assert.Equal(AircraftFlightStatusProcessingResult.Stale, resultB);

        Assert.Equal(0, conflictsA.Count);
        Assert.Equal(1, conflictsB.Count);

        await using var verificationDb = _postgres.CreateDbContext();

        var aircraft = await verificationDb.Aircraft
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == aircraftId,
                TestContext.Current.CancellationToken);

        Assert.Equal(Status.EnRoute, aircraft.Status);
        Assert.Equal("EHAM", aircraft.EnRouteTo);
        Assert.Null(aircraft.ParkedAt);
        Assert.Equal(t0.AddMinutes(10), aircraft.UpdatedAt);

        // Both events were handled successfully:
        // A was applied; B was recognized as stale.
        Assert.Equal(2, await verificationDb.InboxMessages.CountAsync(
            TestContext.Current.CancellationToken));

        Assert.True(await verificationDb.InboxMessages.AnyAsync(
            x => x.EventId == eventIdA,
            TestContext.Current.CancellationToken));

        Assert.True(await verificationDb.InboxMessages.AnyAsync(
            x => x.EventId == eventIdB,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Concurrent_aircraft_updates_should_reapply_newer_event_after_conflict()
    {
        // Arrange
        await ResetDatabase();

        var t0 = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        var aircraftId = await CreateAircraft(t0);

        var eventIdA = Guid.NewGuid();
        var eventIdB = Guid.NewGuid();

        var conflictsA = new ConcurrencyConflictCounter();
        var conflictsB = new ConcurrencyConflictCounter();

        await using var dbA = _postgres.CreateDbContext(conflictsA);
        await using var dbB = _postgres.CreateDbContext(conflictsB);

        await dbA.Aircraft.SingleAsync(
            x => x.Id == aircraftId,
            TestContext.Current.CancellationToken);

        await dbB.Aircraft.SingleAsync(
            x => x.Id == aircraftId,
            TestContext.Current.CancellationToken);

        var processorA = new AircraftFlightStatusProcessor(dbA);
        var processorB = new AircraftFlightStatusProcessor(dbB);

        // Act
        var resultA = await processorA.ProcessAsync(
            eventIdA,
            aircraftId,
            Status.EnRoute,
            "EHAM",
            t0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        var resultB = await processorB.ProcessAsync(
            eventIdB,
            aircraftId,
            Status.Parked,
            "RKSI",
            t0.AddMinutes(10),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, resultA);
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, resultB);

        Assert.Equal(0, conflictsA.Count);
        Assert.Equal(1, conflictsB.Count);

        await using var verificationDb = _postgres.CreateDbContext();

        var aircraft = await verificationDb.Aircraft
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == aircraftId,
                TestContext.Current.CancellationToken);

        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("RKSI", aircraft.ParkedAt);
        Assert.Null(aircraft.EnRouteTo);
        Assert.Equal(t0.AddMinutes(10), aircraft.UpdatedAt);

        Assert.Equal(2, await verificationDb.InboxMessages.CountAsync(
            TestContext.Current.CancellationToken));

        Assert.True(await verificationDb.InboxMessages.AnyAsync(
            x => x.EventId == eventIdA,
            TestContext.Current.CancellationToken));

        Assert.True(await verificationDb.InboxMessages.AnyAsync(
            x => x.EventId == eventIdB,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Processing_same_event_twice_should_return_duplicate_without_changing_aircraft()
    {
        // Arrange
        await ResetDatabase();

        var t0 = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        var aircraftId = await CreateAircraft(t0);
        var eventId = Guid.NewGuid();

        // Act
        await using var dbA = _postgres.CreateDbContext();

        var resultA = await new AircraftFlightStatusProcessor(dbA)
            .ProcessAsync(
                eventId,
                aircraftId,
                Status.EnRoute,
                "EHAM",
                t0.AddMinutes(5),
                TestContext.Current.CancellationToken);

        // Simulate SQS redelivery using a fresh DbContext.
        await using var dbB = _postgres.CreateDbContext();

        var resultB = await new AircraftFlightStatusProcessor(dbB)
            .ProcessAsync(
                eventId,
                aircraftId,
                Status.EnRoute,
                "EHAM",
                t0.AddMinutes(5),
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, resultA);
        Assert.Equal(AircraftFlightStatusProcessingResult.Duplicate, resultB);

        await using var verificationDb = _postgres.CreateDbContext();

        var aircraft = await verificationDb.Aircraft
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == aircraftId,
                TestContext.Current.CancellationToken);

        Assert.Equal(Status.EnRoute, aircraft.Status);
        Assert.Equal("EHAM", aircraft.EnRouteTo);
        Assert.Null(aircraft.ParkedAt);
        Assert.Equal(t0.AddMinutes(5), aircraft.UpdatedAt);

        Assert.Equal(1, await verificationDb.InboxMessages.CountAsync(
            x => x.EventId == eventId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Failed_aircraft_transition_should_not_commit_inbox_record()
    {
        // Arrange
        await ResetDatabase();

        var t0 = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        var aircraftId = await CreateAircraft(t0);
        var eventId = Guid.NewGuid();

        await using var db = _postgres.CreateDbContext();

        var processor = new AircraftFlightStatusProcessor(db);

        // Act: the invalid destination causes domain validation to fail
        // after the inbox INSERT has executed.
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => processor.ProcessAsync(eventId, aircraftId, Status.EnRoute, "", t0.AddMinutes(5), TestContext.Current.CancellationToken));

        Assert.Equal("destination", exception.ParamName);

        // Assert: the transaction was not committed.
        await using var verificationDb = _postgres.CreateDbContext();

        Assert.False(await verificationDb.InboxMessages.AnyAsync(
            x => x.EventId == eventId,
            TestContext.Current.CancellationToken));

        var aircraft = await verificationDb.Aircraft
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == aircraftId,
                TestContext.Current.CancellationToken);

        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("CYVR", aircraft.ParkedAt);
        Assert.Null(aircraft.EnRouteTo);
        Assert.Equal(t0, aircraft.UpdatedAt);
    }

    [Fact]
    public async Task Stale_event_should_be_recorded_in_inbox_and_then_deduplicated()
    {
        // Arrange
        await ResetDatabase();

        var t0 = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        var aircraftId = await CreateAircraft(t0);
        var eventId = Guid.NewGuid();

        // Act
        await using var dbA = _postgres.CreateDbContext();

        var resultA = await new AircraftFlightStatusProcessor(dbA)
            .ProcessAsync(
                eventId,
                aircraftId,
                Status.EnRoute,
                "EHAM",
                t0,
                TestContext.Current.CancellationToken);

        await using var dbB = _postgres.CreateDbContext();

        var resultB = await new AircraftFlightStatusProcessor(dbB)
            .ProcessAsync(
                eventId,
                aircraftId,
                Status.EnRoute,
                "EHAM",
                t0,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AircraftFlightStatusProcessingResult.Stale, resultA);
        Assert.Equal(AircraftFlightStatusProcessingResult.Duplicate, resultB);

        await using var verificationDb = _postgres.CreateDbContext();

        Assert.Equal(1, await verificationDb.InboxMessages.CountAsync(
            x => x.EventId == eventId,
            TestContext.Current.CancellationToken));

        var aircraft = await verificationDb.Aircraft
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == aircraftId,
                TestContext.Current.CancellationToken);

        Assert.Equal(Status.Parked, aircraft.Status);
        Assert.Equal("CYVR", aircraft.ParkedAt);
        Assert.Null(aircraft.EnRouteTo);
        Assert.Equal(t0, aircraft.UpdatedAt);
    }

    private async Task<Guid> CreateAircraft(DateTimeOffset createdAt)
    {
        var args = new AircraftCreationArgs
        {
            TailNumber = "C-FJRN",
            EquipmentCode = "B78X",
            DryOperatingWeight = new Weight(135500),
            MaximumTakeoffWeight = new Weight(254011),
            MaximumLandingWeight = new Weight(201848),
            MaximumZeroFuelWeight = new Weight(192777),
            MaximumFuelWeight = new Weight(101522),
            CreatedAt = createdAt,
            Seats = new SeatLayoutDefinition
            {
                EquipmentType = "B78X",
                EconomyRowsRaw = new Dictionary<string, SeatSectionDefinition>
                {
                    ["1"] = new SeatSectionDefinition
                    {
                        Seats = ['A', 'B'],
                        SeatType = SeatType.Economy
                    }
                }
            },
            AircraftLocationData = new AircraftLocationData(
                Status.Parked, "CYVR", null)
        };

        var aircraft = Core.Models.Aircraft.Create(args);

        await using var db = _postgres.CreateDbContext();

        db.Aircraft.Add(aircraft);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return aircraft.Id;
    }

    private async Task ResetDatabase()
    {
        await using var db = _postgres.CreateDbContext();

        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                aircraft.aircraft,
                aircraft.inbox_messages,
                aircraft.outbox_messages
            CASCADE;
            """,
            TestContext.Current.CancellationToken);
    }
}
