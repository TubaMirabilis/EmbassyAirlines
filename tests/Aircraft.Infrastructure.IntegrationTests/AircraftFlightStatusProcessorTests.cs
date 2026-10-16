using Aircraft.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class AircraftFlightStatusProcessorTests
{
    private readonly PostgreSqlFixture _postgres;
    public AircraftFlightStatusProcessorTests(PostgreSqlFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task Concurrent_aircraft_updates_should_discard_older_event_after_conflict()
    {
        // Arrange
        await ResetDatabase();
        var t0 = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        var aircraftId = await CreateAircraft(t0);
        var conflictsA = new ConcurrencyConflictCounter();
        var conflictsB = new ConcurrencyConflictCounter();
        await using var dbA = _postgres.CreateDbContext(conflictsA);
        await using var dbB = _postgres.CreateDbContext(conflictsB);

        // Both contexts load the aircraft before either saves, so each processor's lookup is served from its
        // own context's snapshot. Context B therefore still sees UpdatedAt = T0 and its staleness check passes.
        await dbA.Aircraft.SingleAsync(x => x.Id == aircraftId, TestContext.Current.CancellationToken);
        await dbB.Aircraft.SingleAsync(x => x.Id == aircraftId, TestContext.Current.CancellationToken);
        var processorA = new AircraftFlightStatusProcessor(dbA);
        var processorB = new AircraftFlightStatusProcessor(dbB);

        // Act
        var resultA = await processorA.ProcessAsync(
            aircraftId,
            Status.EnRoute,
            "EHAM",
            t0.AddMinutes(10),
            TestContext.Current.CancellationToken);

        var resultB = await processorB.ProcessAsync(
            aircraftId,
            Status.Parked,
            "RKSI",
            t0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, resultA);
        Assert.Equal(AircraftFlightStatusProcessingResult.Stale, resultB);

        // A saved first against a current row version; B's first save hit A's commit, so it reloaded, saw A's
        // newer UpdatedAt, and discarded its event on the retry's staleness check rather than on the first pass.
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
    }

    [Fact]
    public async Task Concurrent_aircraft_updates_should_reapply_newer_event_after_conflict()
    {
        // Arrange
        await ResetDatabase();
        var t0 = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        var aircraftId = await CreateAircraft(t0);
        var conflictsA = new ConcurrencyConflictCounter();
        var conflictsB = new ConcurrencyConflictCounter();
        await using var dbA = _postgres.CreateDbContext(conflictsA);
        await using var dbB = _postgres.CreateDbContext(conflictsB);

        // Both contexts load the aircraft before either saves, so each processor's lookup is served from its
        // own context's snapshot. Context B therefore still sees UpdatedAt = T0 and its staleness check passes.
        await dbA.Aircraft.SingleAsync(x => x.Id == aircraftId, TestContext.Current.CancellationToken);
        await dbB.Aircraft.SingleAsync(x => x.Id == aircraftId, TestContext.Current.CancellationToken);
        var processorA = new AircraftFlightStatusProcessor(dbA);
        var processorB = new AircraftFlightStatusProcessor(dbB);

        // Act
        var resultA = await processorA.ProcessAsync(
            aircraftId,
            Status.EnRoute,
            "EHAM",
            t0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        var resultB = await processorB.ProcessAsync(
            aircraftId,
            Status.Parked,
            "RKSI",
            t0.AddMinutes(10),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, resultA);
        Assert.Equal(AircraftFlightStatusProcessingResult.Applied, resultB);

        // A saved first against a current row version; B's first save hit A's commit, so it reloaded and retried once.
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
                    ["1"] = new SeatSectionDefinition { Seats = ['A', 'B'], SeatType = SeatType.Economy }
                }
            },
            AircraftLocationData = new AircraftLocationData(Status.Parked, "CYVR", null)
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
            TRUNCATE TABLE aircraft.aircraft, aircraft.outbox_messages CASCADE;
            """,
            TestContext.Current.CancellationToken);
    }
}
