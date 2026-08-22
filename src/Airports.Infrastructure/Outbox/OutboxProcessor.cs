using System.Text.Json;
using Airports.Infrastructure.Database;
using AWS.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Abstractions;
using Shared.Contracts;

namespace Airports.Infrastructure.Outbox;

public sealed class OutboxProcessor : OutboxProcessorBase, IOutboxProcessor
{
    private static readonly Dictionary<string, Func<IMessagePublisher, string, CancellationToken, Task>> s_publishers =
        new(StringComparer.Ordinal)
        {
            [nameof(AirportCreatedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<AirportCreatedEvent>(content), ct),
            [nameof(AirportUpdatedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<AirportUpdatedEvent>(content), ct)
        };
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessagePublisher _publisher;
    public OutboxProcessor(ApplicationDbContext dbContext,
                           IMessagePublisher publisher,
                           ILogger<OutboxProcessor> logger) : base(logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
    }
    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await GetEligibleMessagesAsync(now, cancellationToken);
        if (messages.Count == 0)
        {
            return 0;
        }
        var publishedCount = 0;
        foreach (var message in messages)
        {
            if (await ProcessMessageAsync(message, now, cancellationToken))
            {
                publishedCount++;
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        LogBatchResult(publishedCount, messages.Count);
        return publishedCount;
    }
    private Task<List<OutboxMessage>> GetEligibleMessagesAsync(DateTime now, CancellationToken cancellationToken) => _dbContext.Set<OutboxMessage>().FromSql(
    $"""
        SELECT * FROM airports.outbox_messages
        WHERE processed_on_utc IS NULL
        AND dead_lettered_on_utc IS NULL
        AND (next_attempt_on_utc IS NULL OR next_attempt_on_utc <= {now})
        ORDER BY created_on_utc
        LIMIT {OutboxConstants.BatchSize}
        FOR UPDATE SKIP LOCKED
    """).ToListAsync(cancellationToken);
    private async Task<bool> ProcessMessageAsync(OutboxMessage message, DateTime now, CancellationToken cancellationToken)
    {
        if (!s_publishers.TryGetValue(message.Name, out var publish))
        {
            RegisterUnknownMessageFailure(message, now);
            return false;
        }
        try
        {
            await publish(_publisher, message.Content, cancellationToken);
            MarkAsProcessed(message, now);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            RegisterPublishFailure(message, e, now);
            return false;
        }
    }
}
