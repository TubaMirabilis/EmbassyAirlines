using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Flights.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class AircraftProjectionWriterTests
{
    private static readonly Instant s_createdAt = Instant.FromUtc(2026, 1, 1, 9, 0);
    private readonly PostgreSqlFixture _postgres;
    public AircraftProjectionWriterTests(PostgreSqlFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task InsertIfAbsentAsync_should_insert_when_aircraft_is_absent()
    {
        var id = Guid.CreateVersion7();
        var tailNumber = "N12345";
        var equipmentCode = "B738";

        await InsertIfAbsentAsync(id, tailNumber, equipmentCode, s_createdAt);

        Assert.Equal((id, tailNumber, equipmentCode, s_createdAt), await GetProjectionAsync(id));
    }

    [Fact]
    public async Task InsertIfAbsentAsync_should_ignore_duplicate_insert()
    {
        var id = Guid.CreateVersion7();
        var tailNumber = "N12345";
        var equipmentCode = "B738";

        await InsertIfAbsentAsync(id, tailNumber, equipmentCode, s_createdAt);
        await InsertIfAbsentAsync(id, tailNumber, equipmentCode, s_createdAt);

        Assert.Equal((id, tailNumber, equipmentCode, s_createdAt), await GetProjectionAsync(id));
    }


    [Fact]
    public async Task InsertIfAbsentAsync_should_not_overwrite_existing_aircraft()
    {
        var id = Guid.CreateVersion7();

        await InsertIfAbsentAsync(id, "N12345", "B738", s_createdAt);
        await InsertIfAbsentAsync(id, "N67890", "A320", s_createdAt.Plus(Duration.FromHours(1)));

        Assert.Equal(
            (id, "N12345", "B738", s_createdAt),
            await GetProjectionAsync(id));
    }

    private async Task InsertIfAbsentAsync(Guid id, string tailNumber, string equipmentCode, Instant createdAt)
    {
        await using var db = _postgres.CreateDbContext();
        var writer = new AircraftProjectionWriter(db);
        await writer.InsertIfAbsentAsync(id, tailNumber, equipmentCode, createdAt, TestContext.Current.CancellationToken);
    }

    private async Task<(Guid Id, string TailNumber, string EquipmentCode, Instant CreatedAt)> GetProjectionAsync(Guid id)
    {
        await using var db = _postgres.CreateDbContext();
        var aircraft = await db.Aircraft
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { a.Id, a.TailNumber, a.EquipmentCode, a.CreatedAt })
            .SingleAsync(TestContext.Current.CancellationToken);
        return (aircraft.Id, aircraft.TailNumber, aircraft.EquipmentCode, aircraft.CreatedAt);
    }
}
