using System.Collections.Concurrent;
using AWS.Messaging;
using AWS.Messaging.Publishers;

namespace Aircraft.Infrastructure.IntegrationTests;

/// <summary>
/// Succeeds and records each message unless a failure has been queued, in which case the next
/// attempt consumes that failure and throws it. Queue a transient exception to schedule a retry, or
/// one the processor treats as unrecoverable (<see cref="System.Text.Json.JsonException"/>,
/// <see cref="NotSupportedException"/>) to dead-letter the message.
/// </summary>
public sealed class ConfigurableMessagePublisher : IMessagePublisher
{
    private readonly ConcurrentQueue<Exception> _failures = new();
    private readonly ConcurrentQueue<object> _published = new();
    private int _attemptCount;

    public int AttemptCount => Volatile.Read(ref _attemptCount);

    public IReadOnlyCollection<object> Published => [.. _published];

    public void FailNextAttemptWith(Exception exception) => _failures.Enqueue(exception);

    public Task<IPublishResponse> PublishAsync<T>(
        T message,
        CancellationToken token = default)
    {
        Interlocked.Increment(ref _attemptCount);

        if (_failures.TryDequeue(out var failure))
        {
            return Task.FromException<IPublishResponse>(failure);
        }

        _published.Enqueue(message!);

        IPublishResponse response = new FakePublishResponse
        {
            MessageId = Guid.NewGuid().ToString()
        };

        return Task.FromResult(response);
    }
}
