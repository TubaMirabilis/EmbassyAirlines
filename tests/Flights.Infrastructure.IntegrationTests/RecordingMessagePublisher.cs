using System.Collections.Concurrent;
using AWS.Messaging;
using AWS.Messaging.Publishers;

namespace Flights.Infrastructure.IntegrationTests;

public sealed class RecordingMessagePublisher : IMessagePublisher
{
    private readonly ConcurrentQueue<object> _published = new();

    public IReadOnlyCollection<object> Published => [.. _published];

    public Task<IPublishResponse> PublishAsync<T>(
        T message,
        CancellationToken token = default)
    {
        _published.Enqueue(message!);

        IPublishResponse response = new FakePublishResponse
        {
            MessageId = Guid.NewGuid().ToString()
        };

        return Task.FromResult(response);
    }
}
