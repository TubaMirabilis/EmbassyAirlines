using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Flights.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class AirportProjectionWriterTests
{
    private static readonly Instant s_createdAt = Instant.FromUtc(2026, 1, 1, 9, 0);
    private readonly PostgreSqlFixture _postgres;
    public AirportProjectionWriterTests(PostgreSqlFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task ApplyAsync_should_insert_version_1_when_airport_is_absent()
    {
        var version1 = CreateHeathrowSnapshot(Guid.CreateVersion7(), 1);

        await ApplyAsync(version1);

        Assert.Equal(version1, await GetProjectionAsync(version1.Id));
    }

    [Fact]
    public async Task ApplyAsync_should_update_to_version_2_and_not_revert_to_version_1()
    {
        var version1 = CreateHeathrowSnapshot(Guid.CreateVersion7(), 1);
        var version2 = CreateDublinSnapshot(version1.Id, 2);
        await ApplyAsync(version1);

        await ApplyAsync(version2);

        Assert.Equal(version2, await GetProjectionAsync(version1.Id));

        await ApplyAsync(version1);

        Assert.Equal(version2, await GetProjectionAsync(version1.Id));
    }

    [Fact]
    public async Task ApplyAsync_should_insert_version_3_when_airport_is_absent_and_not_overwrite_with_version_1()
    {
        var version3 = CreateDublinSnapshot(Guid.CreateVersion7(), 3);
        var version1 = CreateHeathrowSnapshot(version3.Id, 1);

        await ApplyAsync(version3);

        Assert.Equal(version3, await GetProjectionAsync(version3.Id));

        await ApplyAsync(version1);

        Assert.Equal(version3, await GetProjectionAsync(version3.Id));
    }

    [Fact]
    public async Task ApplyAsync_should_ignore_duplicate_version()
    {
        var id = Guid.CreateVersion7();
        var version1 = CreateHeathrowSnapshot(id, 1);

        await ApplyAsync(version1);
        await ApplyAsync(version1);

        Assert.Equal(version1, await GetProjectionAsync(id));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task ApplyAsync_should_reject_non_positive_version_without_writing(long version)
    {
        var snapshot = CreateHeathrowSnapshot(Guid.CreateVersion7(), version);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ApplyAsync(snapshot));

        await using var db = _postgres.CreateDbContext();
        Assert.False(await db.Airports.AnyAsync(a => a.Id == snapshot.Id, TestContext.Current.CancellationToken));
    }
    private static AirportSnapshot CreateHeathrowSnapshot(Guid id, long version) =>
        new(id, version, "London Heathrow", "EGLL", "LHR", "Europe/London", s_createdAt, s_createdAt.Plus(Duration.FromHours(version)));
    private static AirportSnapshot CreateDublinSnapshot(Guid id, long version) =>
        new(id, version, "Dublin Airport", "EIDW", "DUB", "Europe/Dublin", s_createdAt, s_createdAt.Plus(Duration.FromHours(version)));
    private async Task ApplyAsync(AirportSnapshot snapshot)
    {
        await using var db = _postgres.CreateDbContext();
        var writer = new AirportProjectionWriter(db);
        await writer.ApplyAsync(snapshot, TestContext.Current.CancellationToken);
    }
    private async Task<AirportSnapshot> GetProjectionAsync(Guid id)
    {
        await using var db = _postgres.CreateDbContext();
        return await db.Airports
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AirportSnapshot(a.Id, a.Version, a.Name, a.IcaoCode, a.IataCode, a.TimeZoneId, a.CreatedAt, a.UpdatedAt))
            .SingleAsync(TestContext.Current.CancellationToken);
    }
}
