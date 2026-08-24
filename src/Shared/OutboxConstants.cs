namespace Shared;

public static class OutboxConstants
{
    public static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(30);
    public const int BatchSize = 100;
    public static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan ClaimSafetyMargin = TimeSpan.FromSeconds(10);
    public const int MaxRetryAttempts = 5;
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(3600);
}
