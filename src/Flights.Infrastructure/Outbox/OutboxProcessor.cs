using System.Text.Json;
using AWS.Messaging;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Abstractions;
using Shared.Contracts;

namespace Flights.Infrastructure.Outbox;

public sealed class OutboxProcessor : OutboxProcessorBase, IOutboxProcessor
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
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;
    public OutboxProcessor(ApplicationDbContext dbContext,
                           IMessagePublisher publisher,
                           ILogger<OutboxProcessor> logger) : base(logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _logger = logger;
    }
    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await _dbContext.Set<OutboxMessage>()
                                       .FromSql(
                                           $"""
                                            SELECT * FROM flights.outbox_messages
                                            WHERE processed_on_utc IS NULL
                                              AND dead_lettered_on_utc IS NULL
                                              AND (next_attempt_on_utc IS NULL OR next_attempt_on_utc <= {now})
                                            ORDER BY created_on_utc
                                            LIMIT {OutboxConstants.BatchSize}
                                            FOR UPDATE SKIP LOCKED
                                            """)
                                       .ToListAsync(cancellationToken);
        if (messages.Count == 0)
        {
            return 0;
        }
        var publishedCount = 0;
        foreach (var message in messages)
        {
            if (!s_publishers.TryGetValue(message.Name, out var publish))
            {
                _logger.LogError("No publisher is registered for outbox message {MessageId} of type {MessageType}", message.Id, message.Name);
                RegisterFailure(message, $"No publisher is registered for message type {message.Name}", unrecoverable: true, now);
                continue;
            }
            try
            {
                await publish(_publisher, message.Content, cancellationToken);
                message.ProcessedOnUtc = now;
                message.Error = null;
                message.NextAttemptOnUtc = null;
                publishedCount++;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                var unrecoverable = e is JsonException or NotSupportedException;
                _logger.LogError(e, "Failed to publish outbox message {MessageId} of type {MessageType} on attempt {Attempt}", message.Id, message.Name, message.RetryCount + 1);
                RegisterFailure(message, e.Message, unrecoverable, now);
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Published {PublishedCount} of {BatchCount} eligible outbox messages", publishedCount, messages.Count);
        }
        return publishedCount;
    }
}
