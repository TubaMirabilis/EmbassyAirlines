using System.Text.Json;
using Aircraft.Infrastructure.Database;
using AWS.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Abstractions;
using Shared.Contracts;

namespace Aircraft.Infrastructure.Outbox;

public sealed class OutboxProcessor : OutboxProcessorBase<IMessagePublisher>, IOutboxProcessor
{
    private static readonly Dictionary<string, Func<IMessagePublisher, string, CancellationToken, Task>> s_publishers =
        new(StringComparer.Ordinal)
        {
            [nameof(AircraftCreatedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<AircraftCreatedEvent>(content), ct)
        };
    private readonly ApplicationDbContext _dbContext;
    public OutboxProcessor(ApplicationDbContext dbContext,
                           IMessagePublisher publisher,
                           ILogger<OutboxProcessor> logger) : base(publisher, logger) => _dbContext = dbContext;
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
        SELECT * FROM aircraft.outbox_messages
        WHERE processed_on_utc IS NULL
        AND dead_lettered_on_utc IS NULL
        AND (next_attempt_on_utc IS NULL OR next_attempt_on_utc <= {now})
        ORDER BY created_on_utc
        LIMIT {OutboxConstants.BatchSize}
        FOR UPDATE SKIP LOCKED
    """).ToListAsync(cancellationToken);
    protected override Func<IMessagePublisher, string, CancellationToken, Task>? ResolvePublisher(string messageName)
        => s_publishers.GetValueOrDefault(messageName);
}
