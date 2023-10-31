using EaCommon.Interfaces;
using ErrorOr;
using Mediator;

namespace EaCommon.PipelineBehaviors;

public sealed class MessageValidatorBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IValidate
    where TResponse : IErrorOr
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken ct,
        MessageHandlerDelegate<TMessage, TResponse> next
    )
    {
        var validationResult = await message.ValidateAsync(ct);
        if (validationResult.IsValid)
        {
            return await next(message, ct);
        }
        var errors = validationResult.Errors
            .ConvertAll(validationFailure => Error.Validation(
                validationFailure.PropertyName,
                validationFailure.ErrorMessage));
        return (dynamic)errors;
    }
}