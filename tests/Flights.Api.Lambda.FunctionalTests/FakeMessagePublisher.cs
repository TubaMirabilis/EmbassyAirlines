using AWS.Messaging;
using AWS.Messaging.Publishers;

namespace Flights.Api.Lambda.FunctionalTests;

public class FakeMessagePublisher : IMessagePublisher
{
    public async Task<IPublishResponse> PublishAsync<T>(T message, CancellationToken token = default)
    {
        await Task.CompletedTask;
        return new FakePublishResponse { MessageId = Guid.NewGuid().ToString() };
    }
}
