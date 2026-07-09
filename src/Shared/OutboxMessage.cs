namespace Shared;

public sealed record OutboxMessage(Guid Id, string Name, string Content, DateTime CreatedOnUtc)
{
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextAttemptOnUtc { get; set; }
    public DateTime? DeadLetteredOnUtc { get; set; }
}
