using AWS.Messaging.Publishers;

namespace Flights.Api.Lambda.FunctionalTests;

public class FakePublishResponse : IPublishResponse
{
    public string? MessageId { get; set; }
}
