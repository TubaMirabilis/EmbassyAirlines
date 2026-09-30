using System.Text.Json;
using Aircraft.Core.Models;
using Aircraft.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Shared.Contracts;

namespace Aircraft.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class OutboxPersistenceTests
{
    private readonly PostgreSqlFixture _postgres;
    private readonly TimeProvider _timeProvider;
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    public OutboxPersistenceTests(PostgreSqlFixture postgres)
    {
        _postgres = postgres;
        _timeProvider = TimeProvider.System;
    }

    [Fact]
    public async Task SaveChanges_should_persist_domain_event_to_outbox()
    {
        // Arrange
        await ResetDatabase();
        await using var db = _postgres.CreateDbContext();
        var args = new AircraftCreationArgs
        {
            TailNumber = "C-FJRN",
            EquipmentCode = "B78X",
            DryOperatingWeight = new Weight(135500),
            MaximumTakeoffWeight = new Weight(254011),
            MaximumLandingWeight = new Weight(201848),
            MaximumZeroFuelWeight = new Weight(192777),
            MaximumFuelWeight = new Weight(101522),
            CreatedAt = _timeProvider.GetUtcNow(),
            Seats = JsonSerializer.Deserialize<SeatLayoutDefinition>(SeatLayoutDefinitionJson) ?? throw new JsonException(),
            AircraftLocationData = new AircraftLocationData(Status.Parked, "CYVR", null)
        };
        var aircraft = Core.Models.Aircraft.Create(args);
        var domainEvent = Assert.Single(aircraft.DomainEvents);
        db.Aircraft.Add(aircraft);

        // Act
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(aircraft.DomainEvents);
        await using var verificationDb = _postgres.CreateDbContext();
        var message = await verificationDb.Set<OutboxMessage>().SingleAsync(x => x.Id == domainEvent.Id, TestContext.Current.CancellationToken);
        Assert.Equal(nameof(AircraftCreatedEvent), message.Name);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.DeadLetteredOnUtc);
        Assert.Null(message.ClaimId);
        var publishedEvent = JsonSerializer.Deserialize<AircraftCreatedEvent>(message.Content, _options);
        Assert.NotNull(publishedEvent);
        Assert.Equal(domainEvent.Id, publishedEvent.Id);
    }

    [Fact]
    public async Task ProcessAsync_should_publish_and_mark_message_processed()
    {
        await ResetDatabase();
        var @event = new AircraftCreatedEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), "C-FJRN", "B78X");
        await using (var arrangeDb = _postgres.CreateDbContext())
        {
            arrangeDb.Add(new OutboxMessage(@event.Id, nameof(AircraftCreatedEvent), JsonSerializer.Serialize(@event, _options), DateTime.UtcNow));
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
        var aircraftCreated =
            Assert.IsType<AircraftCreatedEvent>(published);
        Assert.Equal(@event.Id, aircraftCreated.Id);
        await using var verificationDb = _postgres.CreateDbContext();
        var message = await verificationDb.Set<OutboxMessage>()
            .SingleAsync(x => x.Id == @event.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.Error);
        Assert.Null(message.NextAttemptOnUtc);
        Assert.Null(message.ClaimId);
        Assert.Null(message.ClaimedUntilUtc);
    }

    [Fact]
    public async Task Concurrent_processors_should_not_publish_same_message()
    {
        await ResetDatabase();
        var eventId = Guid.CreateVersion7();
        await InsertOutboxMessage(eventId);
        var publisher = new BlockingPublisher();
        await using var db1 = _postgres.CreateDbContext();
        await using var db2 = _postgres.CreateDbContext();
        var processor1 = new OutboxProcessor(
            db1,
            publisher,
            NullLogger<OutboxProcessor>.Instance);
        var processor2 = new OutboxProcessor(
            db2,
            publisher,
            NullLogger<OutboxProcessor>.Instance);
        var first = processor1.ProcessAsync(TestContext.Current.CancellationToken);
        await publisher.WaitUntilPublishingAsync();
        var secondResult = await processor2.ProcessAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, secondResult);
        publisher.Release();
        var firstResult = await first;
        Assert.Equal(1, firstResult);
        Assert.Equal(1, publisher.PublishCount);
        await using var verificationDb = _postgres.CreateDbContext();
        var message = await verificationDb.Set<OutboxMessage>()
            .SingleAsync(x => x.Id == eventId, TestContext.Current.CancellationToken);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.ClaimId);
        Assert.Null(message.ClaimedUntilUtc);
    }

    private async Task InsertOutboxMessage(Guid eventId)
    {
        var @event = new AircraftCreatedEvent(eventId, Guid.CreateVersion7(), "C-FJRN", "B78X");
        var message = new OutboxMessage(eventId, nameof(AircraftCreatedEvent), JsonSerializer.Serialize(@event, _options), DateTime.UtcNow);

        await using var db = _postgres.CreateDbContext();
        db.Set<OutboxMessage>().Add(message);
        await db.SaveChangesAsync();
    }

    private async Task ResetDatabase()
    {
        await using var db = _postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE aircraft.outbox_messages CASCADE;
            """);
    }
    private static string SeatLayoutDefinitionJson => """
    {
        "EquipmentType": "B78X",
        "BusinessRows": {
            "1-17": {
                "Seats": ["A", "K"], "SeatType": "Business", "EveryNthRowOnly": 2
            },
            "2-18": {
                "Seats": ["D", "F"], "SeatType": "Business", "EveryNthRowOnly": 2
            }
        },
        "EconomyRows": {
            "19-49": {
                "Seats": ["A", "B", "C", "D", "E", "F", "G", "H", "J"],
                "SeatType": "Economy"
            },
            "50": {
                "Seats": ["A", "B", "C", "D", "F", "G", "H", "J"],
                "SeatType": "Economy"
            },
            "51-52": {
                "Seats": ["A", "B", "D", "E", "F", "G", "J"],
                "SeatType": "Economy"
            }
        }
    }
    """;
}
