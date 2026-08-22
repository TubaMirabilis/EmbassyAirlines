using System.Text.Json;
using AWS.Messaging;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Abstractions;
using Shared.Contracts;

namespace Flights.Infrastructure.Outbox;

public sealed class OutboxProcessor : OutboxProcessorBase<IMessagePublisher>, IOutboxProcessor
{
    private static readonly Dictionary<string, Func<IMessagePublisher, string, CancellationToken, Task>> s_publishers =
        new(StringComparer.Ordinal)
        {
            [nameof(FlightCancelledEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightCancelledEvent>(content), ct),
            [nameof(FlightArrivedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightArrivedEvent>(content), ct),
            [nameof(FlightDelayedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightDelayedEvent>(content), ct),
            [nameof(FlightMarkedAsDelayedEnRouteEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightMarkedAsDelayedEnRouteEvent>(content), ct),
            [nameof(FlightMarkedAsEnRouteEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightMarkedAsEnRouteEvent>(content), ct),
            [nameof(FlightRescheduledEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightRescheduledEvent>(content), ct),
            [nameof(FlightPricingAdjustedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightPricingAdjustedEvent>(content), ct),
            [nameof(AircraftAssignedToFlightEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<AircraftAssignedToFlightEvent>(content), ct),
            [nameof(FlightScheduledEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightScheduledEvent>(content), ct)
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
        SELECT * FROM flights.outbox_messages
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
