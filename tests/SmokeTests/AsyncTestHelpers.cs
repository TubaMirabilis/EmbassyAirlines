namespace SmokeTests;

internal static class AsyncTestHelpers
{
    public static async Task Eventually(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? retryInterval = null,
        CancellationToken ct = default)
    {
        var interval = retryInterval ?? TimeSpan.FromSeconds(5);
        var deadline = DateTimeOffset.UtcNow + timeout;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                if (await condition())
                {
                    return;
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }
            await Task.Delay(remaining < interval ? remaining : interval, ct);
        }
        throw new TimeoutException(
            $"Condition was not met within {timeout}.",
            lastException);
    }
}
