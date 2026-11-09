using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure;

internal static class AircraftConcurrencyRetry
{
    private const int MaxAttempts = 3;

    public static async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt < MaxAttempts; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // Retry with a fresh DbContext.
            }
        }

        // Final attempt: propagate any exception unchanged.
        return await operation(cancellationToken);
    }
}
