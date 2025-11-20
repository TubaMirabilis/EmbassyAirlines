using AWS.Messaging.Publishers;

namespace Flights.Api.FunctionalTests;

public class FakePublishResponse : IPublishResponse
{
    public string? MessageId { get; set; }
}
