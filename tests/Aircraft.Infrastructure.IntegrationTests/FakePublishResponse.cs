using AWS.Messaging.Publishers;

namespace Aircraft.Infrastructure.IntegrationTests;

public class FakePublishResponse : IPublishResponse
{
    public string? MessageId { get; set; }
}
