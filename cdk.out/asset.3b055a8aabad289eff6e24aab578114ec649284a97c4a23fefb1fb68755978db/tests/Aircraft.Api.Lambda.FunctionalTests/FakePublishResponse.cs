using AWS.Messaging.Publishers;

namespace Aircraft.Api.Lambda.FunctionalTests;

public class FakePublishResponse : IPublishResponse
{
    public string? MessageId { get; set; }
}
