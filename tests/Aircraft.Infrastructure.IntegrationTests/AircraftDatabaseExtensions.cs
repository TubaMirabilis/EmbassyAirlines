using Aircraft.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure.IntegrationTests;

// Seeds and inspects the aircraft schema for the inbox processor tests.
internal static class AircraftDatabaseExtensions
{
    public static async Task ResetAsync(this PostgreSqlFixture postgres)
    {
        await using var db = postgres.CreateDbContext();

        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                aircraft.aircraft,
                aircraft.completed_flight,
                aircraft.inbox_messages,
                aircraft.outbox_messages
            CASCADE;
            """,
            TestContext.Current.CancellationToken);
    }

    // Creates an aircraft parked at CYVR.
    public static async Task<Guid> CreateAircraftAsync(
        this PostgreSqlFixture postgres,
        string tailNumber,
        DateTimeOffset createdAt)
    {
        var args = new AircraftCreationArgs
        {
            CreatedAt = createdAt,
            DryOperatingWeight = new Weight(135500),
            EquipmentCode = "B78X",
            InitialBlockTime = TimeSpan.FromHours(0),
            MaximumFuelWeight = new Weight(101522),
            MaximumLandingWeight = new Weight(201848),
            MaximumTakeoffWeight = new Weight(254011),
            MaximumZeroFuelWeight = new Weight(192777),
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
            Situation = AircraftSituation.Parked("CYVR", createdAt),
            TailNumber = tailNumber
        };

        var aircraft = Core.Models.Aircraft.Create(args);

        await using var db = postgres.CreateDbContext();
        db.Aircraft.Add(aircraft);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return aircraft.Id;
    }

    // Stands in for a modification unrelated to flight movements, which
    // moves the aircraft's UpdatedAt but not its Situation.EffectiveAt.
    public static async Task SetUpdatedAtAsync(
        this PostgreSqlFixture postgres,
        Guid aircraftId,
        DateTimeOffset updatedAt)
    {
        await using var db = postgres.CreateDbContext();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE aircraft.aircraft SET updated_at = {updatedAt} WHERE id = {aircraftId};",
            TestContext.Current.CancellationToken);
    }

    public static async Task<Core.Models.Aircraft> GetAircraftWithHistoryAsync(
        this PostgreSqlFixture postgres,
        Guid aircraftId)
    {
        await using var db = postgres.CreateDbContext();

        return await db.Aircraft
            .AsNoTracking()
            .Include(a => a.CompletedFlights)
            .SingleAsync(a => a.Id == aircraftId, TestContext.Current.CancellationToken);
    }

    public static async Task<bool> InboxContainsAsync(this PostgreSqlFixture postgres, Guid eventId)
    {
        await using var db = postgres.CreateDbContext();

        return await db.InboxMessages.AnyAsync(
            x => x.EventId == eventId,
            TestContext.Current.CancellationToken);
    }
}
