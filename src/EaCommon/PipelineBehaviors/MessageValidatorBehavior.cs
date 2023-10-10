using EaCommon.Exceptions;
using EaCommon.Interfaces;
using Mediator;

namespace EaCommon.PipelineBehaviors;

public sealed class MessageValidatorBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IValidate
{
    public ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken ct,
        MessageHandlerDelegate<TMessage, TResponse> next
    )
    {
        if (!message.IsValid(out var validationError))
        {
            throw new ValidationException(validationError);
        }
        return next(message, ct);
    }
}