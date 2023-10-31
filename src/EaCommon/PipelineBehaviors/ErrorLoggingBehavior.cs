using Mediator;
using Microsoft.Extensions.Logging;

namespace EaCommon.PipelineBehaviors;

public sealed class ErrorLoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<ErrorLoggingBehavior<TMessage, TResponse>> _logger;
    public ErrorLoggingBehavior(ILogger<ErrorLoggingBehavior<TMessage, TResponse>> logger)
    {
        _logger = logger;
    }
    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken ct,
        MessageHandlerDelegate<TMessage, TResponse> next
    )
    {
        try
        {
            return await next(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message of type {messageType}", message.GetType().Name);
            throw;
        }
    }
}