using AWS.Messaging.Publishers;

namespace Airports.Api.Lambda.FunctionalTests;

public class FakePublishResponse : IPublishResponse
{
    public string? MessageId { get; set; }
}
