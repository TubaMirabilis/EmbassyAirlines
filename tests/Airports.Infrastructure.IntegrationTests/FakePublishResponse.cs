using AWS.Messaging.Publishers;

namespace Airports.Infrastructure.IntegrationTests;

public class FakePublishResponse : IPublishResponse
{
    public string? MessageId { get; set; }
}
