using Microsoft.Extensions.Logging;

namespace Shared;

public abstract class OutboxProcessorBase<TPublisher> : OutboxProcessorBase
{
    protected OutboxProcessorBase(TPublisher publisher, ILogger logger) : base(logger) => Publisher = publisher;
    protected TPublisher Publisher { get; }
    protected abstract Func<TPublisher, string, CancellationToken, Task>? ResolvePublisher(string messageName);
    protected async Task<bool> ProcessMessageAsync(OutboxMessage message, DateTime now, CancellationToken cancellationToken)
    {
        var publish = ResolvePublisher(message.Name);
        if (publish is null)
        {
            RegisterUnknownMessageFailure(message, now);
            return false;
        }
        try
        {
            await publish(Publisher, message.Content, cancellationToken);
            MarkAsProcessed(message, now);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            RegisterPublishFailure(message, e, now);
            return false;
        }
    }
}
