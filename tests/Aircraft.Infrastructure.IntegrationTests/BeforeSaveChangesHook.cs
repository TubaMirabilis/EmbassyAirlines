using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aircraft.Infrastructure.IntegrationTests;

// Runs an action before the next N saves, e.g. to commit a competing
// write between a processor loading an aggregate and saving it.
public sealed class BeforeSaveChangesHook : SaveChangesInterceptor
{
    private readonly Func<CancellationToken, Task> _action;
    private int _remainingInvocations;

    public BeforeSaveChangesHook(int invocations, Func<CancellationToken, Task> action)
    {
        _remainingInvocations = invocations;
        _action = action;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_remainingInvocations > 0)
        {
            _remainingInvocations--;
            await _action(cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
