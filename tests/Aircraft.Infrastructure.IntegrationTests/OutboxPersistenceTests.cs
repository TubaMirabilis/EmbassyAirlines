using System.Text.Json;
using Aircraft.Core.Models;
using Aircraft.Infrastructure.Outbox;
using AWS.Messaging;
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
        try
        {
            await publisher.WaitUntilPublishingAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            var secondResult = await processor2.ProcessAsync(TestContext.Current.CancellationToken);
            Assert.Equal(0, secondResult);
        }
        finally
        {
            // Unblock the first processor and let it finish before db1 is disposed, without
            // letting its own failure mask whatever exception is already propagating.
            publisher.Release();
            await ((Task)first).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        }

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

    [Fact]
    public async Task ProcessAsync_should_reclaim_and_publish_message_whose_claim_has_expired()
    {
        // Arrange: a worker claimed the message and then died without recording an outcome.
        await ResetDatabase();
        var eventId = Guid.CreateVersion7();
        await InsertOutboxMessage(eventId);
        var staleClaimId = Guid.CreateVersion7();
        await using (var db = _postgres.CreateDbContext())
        {
            await db.Database.ExecuteSqlAsync(
                $"""
                UPDATE aircraft.outbox_messages
                SET claim_id = {staleClaimId}, claimed_until_utc = now() - interval '1 second'
                WHERE id = {eventId}
                """,
                TestContext.Current.CancellationToken);
        }

        var publisher = new RecordingMessagePublisher();

        // Act
        var count = await ProcessOnceAsync(publisher);

        // Assert
        Assert.Equal(1, count);
        var published = Assert.IsType<AircraftCreatedEvent>(Assert.Single(publisher.Published));
        Assert.Equal(eventId, published.Id);
        var message = await GetOutboxMessage(eventId);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.ClaimId);
        Assert.Null(message.ClaimedUntilUtc);
    }

    [Fact]
    public async Task Processor_whose_claim_expires_mid_batch_should_not_record_outcome_and_should_abandon_batch()
    {
        // Arrange
        await ResetDatabase();
        var firstId = Guid.CreateVersion7();
        var secondId = Guid.CreateVersion7();
        await InsertOutboxMessage(firstId, DateTime.UtcNow.AddSeconds(-1));
        await InsertOutboxMessage(secondId, DateTime.UtcNow);
        var blockingPublisher = new BlockingPublisher();
        await using var blockedDb = _postgres.CreateDbContext();
        var blockedProcessor = new OutboxProcessor(blockedDb, blockingPublisher, NullLogger<OutboxProcessor>.Instance);

        // Act: the processor claims both messages, then overruns its lease while publishing the first.
        var blocked = blockedProcessor.ProcessAsync(TestContext.Current.CancellationToken);
        try
        {
            await blockingPublisher.WaitUntilPublishingAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            await ExpireAllClaims();
        }
        finally
        {
            blockingPublisher.Release();
            await ((Task)blocked).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        }

        await blocked;

        // Assert: its outcome was discarded and it did not go on to publish the second message.
        Assert.Equal(1, blockingPublisher.PublishCount);
        foreach (var id in new[] { firstId, secondId })
        {
            var message = await GetOutboxMessage(id);
            Assert.Null(message.ProcessedOnUtc);
            Assert.Null(message.Error);
            Assert.Equal(0, message.RetryCount);
            Assert.Null(message.DeadLetteredOnUtc);
        }

        // Both messages are recoverable by the next processor (the first is published again: at-least-once delivery).
        var recoveryPublisher = new RecordingMessagePublisher();
        var recovered = await ProcessOnceAsync(recoveryPublisher);
        Assert.Equal(2, recovered);
        Assert.Equal(
            [firstId, secondId],
            recoveryPublisher.Published.Cast<AircraftCreatedEvent>().Select(x => x.Id));
        foreach (var id in new[] { firstId, secondId })
        {
            var message = await GetOutboxMessage(id);
            Assert.NotNull(message.ProcessedOnUtc);
            Assert.Null(message.ClaimId);
        }
    }

    [Fact]
    public async Task Processor_that_lost_its_claim_should_not_overwrite_outcome_recorded_by_new_owner()
    {
        // Arrange
        await ResetDatabase();
        var firstId = Guid.CreateVersion7();
        var secondId = Guid.CreateVersion7();
        await InsertOutboxMessage(firstId, DateTime.UtcNow.AddSeconds(-1));
        await InsertOutboxMessage(secondId, DateTime.UtcNow);
        var blockingPublisher = new BlockingPublisher();
        var recoveryPublisher = new RecordingMessagePublisher();
        await using var originalDb = _postgres.CreateDbContext();
        var originalProcessor = new OutboxProcessor(originalDb, blockingPublisher, NullLogger<OutboxProcessor>.Instance);
        OutboxMessage recordedFirst;
        OutboxMessage recordedSecond;

        // Act: the original processor stalls, its claim expires, and a second processor takes over the batch.
        var original = originalProcessor.ProcessAsync(TestContext.Current.CancellationToken);
        try
        {
            await blockingPublisher.WaitUntilPublishingAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            await ExpireAllClaims();
            var recovered = await ProcessOnceAsync(recoveryPublisher);
            Assert.Equal(2, recovered);
            recordedFirst = await GetOutboxMessage(firstId);
            recordedSecond = await GetOutboxMessage(secondId);
        }
        finally
        {
            blockingPublisher.Release();
            await ((Task)original).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        }

        await original;

        // Assert: the new owner's outcomes stand, and the original processor stopped after losing its claim.
        Assert.NotNull(recordedFirst.ProcessedOnUtc);
        Assert.NotNull(recordedSecond.ProcessedOnUtc);
        Assert.Equal(recordedFirst, await GetOutboxMessage(firstId));
        Assert.Equal(recordedSecond, await GetOutboxMessage(secondId));
        Assert.Equal(1, blockingPublisher.PublishCount);
        Assert.Equal(2, recoveryPublisher.Published.Count);
    }

    [Fact]
    public async Task Transient_failure_should_schedule_retry_that_is_not_reclaimed_until_due()
    {
        // Arrange
        await ResetDatabase();
        var eventId = Guid.CreateVersion7();
        await InsertOutboxMessage(eventId);
        var publisher = new ConfigurableMessagePublisher();
        publisher.FailNextAttemptWith(new InvalidOperationException("SNS is unavailable"));

        // Act
        var attemptStartedUtc = DateTime.UtcNow;
        var failedCount = await ProcessOnceAsync(publisher);
        var attemptFinishedUtc = DateTime.UtcNow;

        // Assert: the failure is persisted and a retry scheduled after the base delay.
        Assert.Equal(0, failedCount);
        Assert.Equal(1, publisher.AttemptCount);
        var failed = await GetOutboxMessage(eventId);
        Assert.Equal(1, failed.RetryCount);
        Assert.Equal("SNS is unavailable", failed.Error);
        Assert.Null(failed.ProcessedOnUtc);
        Assert.Null(failed.DeadLetteredOnUtc);
        Assert.Null(failed.ClaimId);
        Assert.Null(failed.ClaimedUntilUtc);
        AssertNextAttemptScheduled(failed, OutboxConstants.BaseRetryDelay, attemptStartedUtc, attemptFinishedUtc);

        // Assert: the message is not eligible before its next attempt is due.
        var earlyCount = await ProcessOnceAsync(publisher);
        Assert.Equal(0, earlyCount);
        Assert.Equal(1, publisher.AttemptCount);
        Assert.Equal(failed, await GetOutboxMessage(eventId));

        // Assert: once due, the retry is published and its failure state cleared.
        await MakeRetryDue(eventId);
        var retriedCount = await ProcessOnceAsync(publisher);
        Assert.Equal(1, retriedCount);
        Assert.Equal(2, publisher.AttemptCount);
        var published = Assert.IsType<AircraftCreatedEvent>(Assert.Single(publisher.Published));
        Assert.Equal(eventId, published.Id);
        var processed = await GetOutboxMessage(eventId);
        Assert.NotNull(processed.ProcessedOnUtc);
        Assert.Null(processed.Error);
        Assert.Null(processed.NextAttemptOnUtc);
        Assert.Null(processed.DeadLetteredOnUtc);
        Assert.Equal(1, processed.RetryCount);
    }

    [Fact]
    public async Task Consecutive_transient_failures_should_back_off_exponentially()
    {
        // Arrange
        await ResetDatabase();
        var eventId = Guid.CreateVersion7();
        await InsertOutboxMessage(eventId);
        var publisher = new ConfigurableMessagePublisher();

        for (var attempt = 1; attempt < OutboxConstants.MaxRetryAttempts; attempt++)
        {
            // Act
            publisher.FailNextAttemptWith(new InvalidOperationException($"Failure {attempt}"));
            var attemptStartedUtc = DateTime.UtcNow;
            await ProcessOnceAsync(publisher);
            var attemptFinishedUtc = DateTime.UtcNow;

            // Assert: base delay doubled for each preceding failure, capped at the maximum delay.
            var expectedDelay = TimeSpan.FromTicks(Math.Min(
                OutboxConstants.BaseRetryDelay.Ticks * (1L << (attempt - 1)),
                OutboxConstants.MaxRetryDelay.Ticks));
            var message = await GetOutboxMessage(eventId);
            Assert.Equal(attempt, message.RetryCount);
            Assert.Equal($"Failure {attempt}", message.Error);
            Assert.Null(message.DeadLetteredOnUtc);
            AssertNextAttemptScheduled(message, expectedDelay, attemptStartedUtc, attemptFinishedUtc);
            await MakeRetryDue(eventId);
        }

        Assert.Equal(OutboxConstants.MaxRetryAttempts - 1, publisher.AttemptCount);
    }

    [Fact]
    public async Task Message_should_be_dead_lettered_after_max_retry_attempts()
    {
        // Arrange
        await ResetDatabase();
        var eventId = Guid.CreateVersion7();
        await InsertOutboxMessage(eventId);
        var publisher = new ConfigurableMessagePublisher();

        // Act: every attempt fails transiently; each retry is made due as soon as it is scheduled.
        for (var attempt = 1; attempt <= OutboxConstants.MaxRetryAttempts; attempt++)
        {
            publisher.FailNextAttemptWith(new InvalidOperationException($"Failure {attempt}"));
            await ProcessOnceAsync(publisher);
            var message = await GetOutboxMessage(eventId);
            Assert.Equal(attempt, message.RetryCount);
            if (attempt < OutboxConstants.MaxRetryAttempts)
            {
                Assert.Null(message.DeadLetteredOnUtc);
                Assert.NotNull(message.NextAttemptOnUtc);
                await MakeRetryDue(eventId);
            }
        }

        // Assert
        Assert.Equal(OutboxConstants.MaxRetryAttempts, publisher.AttemptCount);
        var deadLettered = await GetOutboxMessage(eventId);
        Assert.Equal(OutboxConstants.MaxRetryAttempts, deadLettered.RetryCount);
        Assert.Equal($"Failure {OutboxConstants.MaxRetryAttempts}", deadLettered.Error);
        AssertDeadLettered(deadLettered);
        await AssertExcludedFromProcessing(eventId);
    }

    [Fact]
    public async Task Message_with_unknown_type_should_be_dead_lettered()
    {
        // Arrange
        await ResetDatabase();
        var eventId = Guid.CreateVersion7();
        await InsertOutboxMessage(eventId, "UnknownEvent", "{}", DateTime.UtcNow);
        var publisher = new ConfigurableMessagePublisher();

        // Act
        var count = await ProcessOnceAsync(publisher);

        // Assert
        Assert.Equal(0, count);
        Assert.Equal(0, publisher.AttemptCount);
        var message = await GetOutboxMessage(eventId);
        Assert.Equal(1, message.RetryCount);
        Assert.Equal("No publisher is registered for message type UnknownEvent", message.Error);
        AssertDeadLettered(message);
        await AssertExcludedFromProcessing(eventId);
    }

    [Theory]
    [InlineData("{ not valid json")]
    [InlineData("null")]
    public async Task Message_with_invalid_json_should_be_dead_lettered(string content)
    {
        // Arrange
        await ResetDatabase();
        var eventId = Guid.CreateVersion7();
        await InsertOutboxMessage(eventId, nameof(AircraftCreatedEvent), content, DateTime.UtcNow);
        var publisher = new ConfigurableMessagePublisher();

        // Act
        var count = await ProcessOnceAsync(publisher);

        // Assert: deserialisation fails before the publisher is reached, and is not retried.
        Assert.Equal(0, count);
        Assert.Equal(0, publisher.AttemptCount);
        var message = await GetOutboxMessage(eventId);
        Assert.Equal(1, message.RetryCount);
        Assert.False(string.IsNullOrEmpty(message.Error));
        AssertDeadLettered(message);
        await AssertExcludedFromProcessing(eventId);
    }

    [Theory]
    [InlineData(typeof(JsonException))]
    [InlineData(typeof(NotSupportedException))]
    public async Task Unrecoverable_publish_failure_should_be_dead_lettered_without_retry(Type exceptionType)
    {
        // Arrange
        await ResetDatabase();
        var eventId = Guid.CreateVersion7();
        await InsertOutboxMessage(eventId);
        var publisher = new ConfigurableMessagePublisher();
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Payload cannot be published")!;
        publisher.FailNextAttemptWith(exception);

        // Act
        var count = await ProcessOnceAsync(publisher);

        // Assert
        Assert.Equal(0, count);
        Assert.Equal(1, publisher.AttemptCount);
        var message = await GetOutboxMessage(eventId);
        Assert.Equal(1, message.RetryCount);
        Assert.Equal("Payload cannot be published", message.Error);
        AssertDeadLettered(message);
        await AssertExcludedFromProcessing(eventId);
    }

    private static void AssertNextAttemptScheduled(OutboxMessage message, TimeSpan expectedDelay, DateTime attemptStartedUtc, DateTime attemptFinishedUtc)
    {
        // PostgreSQL stores timestamps to the microsecond, so allow for rounding at either bound.
        var tolerance = TimeSpan.FromMilliseconds(1);
        Assert.NotNull(message.NextAttemptOnUtc);
        Assert.InRange(
            message.NextAttemptOnUtc.Value,
            attemptStartedUtc + expectedDelay - tolerance,
            attemptFinishedUtc + expectedDelay + tolerance);
    }

    private static void AssertDeadLettered(OutboxMessage message)
    {
        Assert.NotNull(message.DeadLetteredOnUtc);
        Assert.Null(message.NextAttemptOnUtc);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.ClaimId);
        Assert.Null(message.ClaimedUntilUtc);
    }

    private async Task AssertExcludedFromProcessing(Guid eventId)
    {
        var before = await GetOutboxMessage(eventId);
        var publisher = new ConfigurableMessagePublisher();

        var count = await ProcessOnceAsync(publisher);

        Assert.Equal(0, count);
        Assert.Equal(0, publisher.AttemptCount);
        Assert.Equal(before, await GetOutboxMessage(eventId));
    }

    private async Task<int> ProcessOnceAsync(IMessagePublisher publisher)
    {
        await using var db = _postgres.CreateDbContext();
        var processor = new OutboxProcessor(db, publisher, NullLogger<OutboxProcessor>.Instance);
        return await processor.ProcessAsync(TestContext.Current.CancellationToken);
    }

    private async Task<OutboxMessage> GetOutboxMessage(Guid eventId)
    {
        await using var db = _postgres.CreateDbContext();
        return await db.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == eventId, TestContext.Current.CancellationToken);
    }

    private async Task ExpireAllClaims()
    {
        await using var db = _postgres.CreateDbContext();
        await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE aircraft.outbox_messages
            SET claimed_until_utc = now() - interval '1 second'
            WHERE claim_id IS NOT NULL
            """,
            TestContext.Current.CancellationToken);
    }

    private async Task MakeRetryDue(Guid eventId)
    {
        await using var db = _postgres.CreateDbContext();
        await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE aircraft.outbox_messages
            SET next_attempt_on_utc = now() - interval '1 second'
            WHERE id = {eventId}
            """,
            TestContext.Current.CancellationToken);
    }

    private Task InsertOutboxMessage(Guid eventId) => InsertOutboxMessage(eventId, DateTime.UtcNow);

    private Task InsertOutboxMessage(Guid eventId, DateTime createdOnUtc)
    {
        var @event = new AircraftCreatedEvent(eventId, Guid.CreateVersion7(), "C-FJRN", "B78X");
        return InsertOutboxMessage(eventId, nameof(AircraftCreatedEvent), JsonSerializer.Serialize(@event, _options), createdOnUtc);
    }

    private async Task InsertOutboxMessage(Guid eventId, string name, string content, DateTime createdOnUtc)
    {
        var message = new OutboxMessage(eventId, name, content, createdOnUtc);

        await using var db = _postgres.CreateDbContext();
        db.Set<OutboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
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
