using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aircraft.Infrastructure.IntegrationTests;

public sealed class ConcurrencyConflictCounter : SaveChangesInterceptor
{
    public int Count { get; private set; }

    public override InterceptionResult ThrowingConcurrencyException(
        ConcurrencyExceptionEventData eventData,
        InterceptionResult result)
    {
        Count++;
        return base.ThrowingConcurrencyException(eventData, result);
    }

    public override ValueTask<InterceptionResult> ThrowingConcurrencyExceptionAsync(
        ConcurrencyExceptionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        Count++;
        return base.ThrowingConcurrencyExceptionAsync(eventData, result, cancellationToken);
    }
}
