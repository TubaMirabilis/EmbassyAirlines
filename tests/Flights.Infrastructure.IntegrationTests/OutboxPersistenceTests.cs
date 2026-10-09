using System.Text.Json;
using Flights.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Shared.Contracts;

namespace Flights.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class OutboxPersistenceTests
{
    private readonly PostgreSqlFixture _postgres;
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    public OutboxPersistenceTests(PostgreSqlFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task ProcessAsync_should_publish_and_mark_message_processed()
    {
        await ResetDatabase();
        var @event = new FlightPricingAdjustedEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), 500, 5000);
        await using (var arrangeDb = _postgres.CreateDbContext())
        {
            arrangeDb.Add(new OutboxMessage(@event.Id, nameof(FlightPricingAdjustedEvent), JsonSerializer.Serialize(@event, _options), DateTime.UtcNow));
            await arrangeDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var publisher = new RecordingMessagePublisher();

        await using var processorDb = _postgres.CreateDbContext();

        var processor = new OutboxProcessor(
            processorDb,
            publisher,
            NullLogger<OutboxProcessor>.Instance);

        // Act
        var count = await processor.ProcessAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, count);
        var published =
            Assert.Single(publisher.Published);
        var flightPricingAdjusted = Assert.IsType<FlightPricingAdjustedEvent>(published);
        Assert.Equal(@event.Id, flightPricingAdjusted.Id);
        await using var verificationDb = _postgres.CreateDbContext();
        var message = await verificationDb.Set<OutboxMessage>()
            .SingleAsync(x => x.Id == @event.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.Error);
        Assert.Null(message.NextAttemptOnUtc);
        Assert.Null(message.ClaimId);
        Assert.Null(message.ClaimedUntilUtc);
    }
    private async Task ResetDatabase()
    {
        await using var db = _postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE flights.outbox_messages CASCADE;
            """);
    }
}
