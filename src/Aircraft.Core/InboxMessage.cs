namespace Aircraft.Core;

public sealed class InboxMessage
{
    public Guid EventId { get; init; }
    public DateTime ProcessedOnUtc { get; init; }
}
