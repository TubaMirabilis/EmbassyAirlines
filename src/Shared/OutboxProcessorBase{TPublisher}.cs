using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Shared;

public abstract class OutboxProcessorBase<TPublisher>
{
    protected OutboxProcessorBase(TPublisher publisher, ILogger logger)
    {
        Logger = logger;
        Publisher = publisher;
    }
    protected ILogger Logger { get; }
    protected TPublisher Publisher { get; }
    protected abstract Func<TPublisher, string, CancellationToken, Task>? ResolvePublisher(string messageName);
    protected async Task<bool> ProcessMessageAsync(OutboxMessage message, DateTime now, CancellationToken cancellationToken)
    {
        var publish = ResolvePublisher(message.Name);
        if (publish is null)
        {
            RegisterUnknownMessageFailure(message, now);
            return false;
        }
        try
        {
            await publish(Publisher, message.Content, cancellationToken);
            MarkAsProcessed(message, now);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            RegisterPublishFailure(message, e, now);
            return false;
        }
    }
    private void RegisterFailure(OutboxMessage message, string error, bool unrecoverable, DateTime now)
    {
        message.Error = error;
        message.RetryCount++;
        if (unrecoverable || message.RetryCount >= OutboxConstants.MaxRetryAttempts)
        {
            message.DeadLetteredOnUtc = now;
            message.NextAttemptOnUtc = null;
            Logger.LogError("Dead-lettered outbox message {MessageId} of type {MessageType} after {RetryCount} attempt(s): {Reason}",
                message.Id, message.Name, message.RetryCount, unrecoverable ? "unrecoverable failure" : "retry limit exceeded");
            return;
        }
        message.NextAttemptOnUtc = now + ComputeBackoff(message.RetryCount);
        Logger.LogWarning("Scheduled retry {RetryCount}/{MaxRetryAttempts} for outbox message {MessageId} at {NextAttemptOnUtc:o}",
            message.RetryCount, OutboxConstants.MaxRetryAttempts, message.Id, message.NextAttemptOnUtc);
    }
    private void RegisterUnknownMessageFailure(OutboxMessage message, DateTime now)
    {
        Logger.LogError("No publisher is registered for outbox message {MessageId} of type {MessageType}", message.Id, message.Name);
        RegisterFailure(message, $"No publisher is registered for message type {message.Name}", unrecoverable: true, now);
    }
    private void RegisterPublishFailure(OutboxMessage message, Exception exception, DateTime now)
    {
        var unrecoverable = exception is JsonException or NotSupportedException;
        Logger.LogError(exception, "Failed to publish outbox message {MessageId} of type {MessageType} on attempt {Attempt}", message.Id, message.Name, message.RetryCount + 1);
        RegisterFailure(message, exception.Message, unrecoverable, now);
    }
    private static void MarkAsProcessed(OutboxMessage message, DateTime now)
    {
        message.ProcessedOnUtc = now;
        message.Error = null;
        message.NextAttemptOnUtc = null;
    }
    private static TimeSpan ComputeBackoff(int retryCount)
    {
        var seconds = Math.Min(OutboxConstants.BaseRetryDelay.TotalSeconds * Math.Pow(2, retryCount - 1), OutboxConstants.MaxRetryDelay.TotalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
