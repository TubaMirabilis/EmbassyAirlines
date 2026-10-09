using AWS.Messaging.Publishers;

namespace Flights.Infrastructure.IntegrationTests;

public class FakePublishResponse : IPublishResponse
{
    public string? MessageId { get; set; }
}
