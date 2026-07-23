using System.Text.Json;
using Aircraft.Infrastructure.Database;
using AWS.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Abstractions;
using Shared.Contracts;

namespace Aircraft.Infrastructure.Outbox;

public sealed class OutboxProcessor : IOutboxProcessor
{
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly Dictionary<string, Func<IMessagePublisher, string, CancellationToken, Task>> s_publishers =
        new(StringComparer.Ordinal)
        {
            [nameof(AircraftCreatedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<AircraftCreatedEvent>(content), ct)
        };
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;
    public OutboxProcessor(ApplicationDbContext dbContext,
                           IMessagePublisher publisher,
                           ILogger<OutboxProcessor> logger)
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
                                            SELECT * FROM aircraft.outbox_messages
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
    private void RegisterFailure(OutboxMessage message, string error, bool unrecoverable, DateTime now)
    {
        message.Error = error;
        message.RetryCount++;
        if (unrecoverable || message.RetryCount >= OutboxConstants.MaxRetryAttempts)
        {
            message.DeadLetteredOnUtc = now;
            message.NextAttemptOnUtc = null;
            _logger.LogError("Dead-lettered outbox message {MessageId} of type {MessageType} after {RetryCount} attempt(s): {Reason}",
                message.Id, message.Name, message.RetryCount, unrecoverable ? "unrecoverable failure" : "retry limit exceeded");
            return;
        }
        message.NextAttemptOnUtc = now + ComputeBackoff(message.RetryCount);
        _logger.LogWarning("Scheduled retry {RetryCount}/{MaxRetryAttempts} for outbox message {MessageId} at {NextAttemptOnUtc:o}",
            message.RetryCount, OutboxConstants.MaxRetryAttempts, message.Id, message.NextAttemptOnUtc);
    }
    private static TimeSpan ComputeBackoff(int retryCount)
    {
        var seconds = Math.Min(OutboxConstants.BaseRetryDelay.TotalSeconds * Math.Pow(2, retryCount - 1), OutboxConstants.MaxRetryDelay.TotalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
    private static T Deserialize<T>(string content)
        => JsonSerializer.Deserialize<T>(content, s_serializerOptions)
           ?? throw new JsonException($"Outbox payload for {typeof(T).Name} deserialised to null.");
}
