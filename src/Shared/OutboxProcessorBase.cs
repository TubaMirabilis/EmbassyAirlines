using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Shared;

public abstract class OutboxProcessorBase
{
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web);
    protected OutboxProcessorBase(ILogger logger) => Logger = logger;
    protected ILogger Logger { get; }
    protected void RegisterFailure(OutboxMessage message, string error, bool unrecoverable, DateTime now)
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
    protected static T Deserialize<T>(string content)
        => JsonSerializer.Deserialize<T>(content, s_serializerOptions)
           ?? throw new JsonException($"Outbox payload for {typeof(T).Name} deserialised to null.");
    private static TimeSpan ComputeBackoff(int retryCount)
    {
        var seconds = Math.Min(OutboxConstants.BaseRetryDelay.TotalSeconds * Math.Pow(2, retryCount - 1), OutboxConstants.MaxRetryDelay.TotalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
