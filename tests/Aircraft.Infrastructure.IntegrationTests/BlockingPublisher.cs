using AWS.Messaging;
using AWS.Messaging.Publishers;

namespace Aircraft.Infrastructure.IntegrationTests;

public sealed class BlockingPublisher : IMessagePublisher
{
    private int _publishCount;

    private readonly TaskCompletionSource _entered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource _release =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int PublishCount => Volatile.Read(ref _publishCount);

    public Task WaitUntilPublishingAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _entered.Task.WaitAsync(timeout, cancellationToken);

    public void Release() => _release.TrySetResult();

    public async Task<IPublishResponse> PublishAsync<T>(
        T message,
        CancellationToken token = default)
    {
        Interlocked.Increment(ref _publishCount);

        _entered.TrySetResult();

        await _release.Task.WaitAsync(token);

        return new FakePublishResponse
        {
            MessageId = Guid.NewGuid().ToString()
        };
    }
}
