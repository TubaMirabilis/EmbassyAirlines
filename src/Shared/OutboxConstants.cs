namespace Shared;

public static class OutboxConstants
{
    public static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(30);
    public const int BatchSize = 100;
    public const int MaxRetryAttempts = 5;
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(3600);
}
