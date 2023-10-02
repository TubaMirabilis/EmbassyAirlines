using EmbassyAirlines.Application.Exceptions;
using EmbassyAirlines.Application.Interfaces;
using Mediator;

namespace EmbassyAirlines.Application.PipelineBehaviors;

public sealed class MessageValidatorBehaviour<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
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