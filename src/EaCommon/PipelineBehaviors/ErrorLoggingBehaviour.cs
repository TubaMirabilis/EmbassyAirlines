using Mediator;
using Microsoft.Extensions.Logging;

namespace EaCommon.PipelineBehaviors;

public sealed class ErrorLoggingBehaviour<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<ErrorLoggingBehaviour<TMessage, TResponse>> _logger;
    public ErrorLoggingBehaviour(ILogger<ErrorLoggingBehaviour<TMessage, TResponse>> logger)
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