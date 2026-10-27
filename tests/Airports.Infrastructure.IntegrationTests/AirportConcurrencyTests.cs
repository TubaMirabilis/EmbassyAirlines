using Airports.Core.Models;
using Microsoft.EntityFrameworkCore;
using Shared.TransactionalOutbox;

namespace Airports.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class AirportConcurrencyTests
{
    private readonly PostgreSqlFixture _postgres;
    public AirportConcurrencyTests(PostgreSqlFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task SaveChanges_should_reject_stale_update_when_airport_was_modified_concurrently()
    {
        // Arrange
        await ResetDatabase();
        var now = DateTimeOffset.UtcNow;
        var airport = Airport.Create("CYVR", "YVR", "Vancouver International Airport", "America/Vancouver", now);
        await using (var arrangeDb = _postgres.CreateDbContext())
        {
            arrangeDb.Airports.Add(airport);
            await arrangeDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var firstDb = _postgres.CreateDbContext();
        await using var secondDb = _postgres.CreateDbContext();
        var firstCopy = await firstDb.Airports.SingleAsync(x => x.Id == airport.Id, TestContext.Current.CancellationToken);
        var secondCopy = await secondDb.Airports.SingleAsync(x => x.Id == airport.Id, TestContext.Current.CancellationToken);

        firstCopy.Update("CYVR", "YVR", "Vancouver Intl", "America/Vancouver", now.AddMinutes(1));
        await firstDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        secondCopy.Update("CYVR", "YVR", "YVR Airport", "America/Vancouver", now.AddMinutes(2));
        var staleEvent = Assert.Single(secondCopy.DomainEvents);

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondDb.SaveChangesAsync(TestContext.Current.CancellationToken));

        // Assert
        var entry = Assert.Single(exception.Entries);
        Assert.Same(secondCopy, entry.Entity);
        await using var verificationDb = _postgres.CreateDbContext();
        var persisted = await verificationDb.Airports.SingleAsync(x => x.Id == airport.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2L, persisted.Version);
        Assert.Equal("Vancouver Intl", persisted.Name);
        Assert.False(await verificationDb.Set<OutboxMessage>().AnyAsync(x => x.Id == staleEvent.Id, TestContext.Current.CancellationToken));
    }
    private async Task ResetDatabase()
    {
        await using var db = _postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE airports.airports, airports.outbox_messages CASCADE;
            """);
    }
}
