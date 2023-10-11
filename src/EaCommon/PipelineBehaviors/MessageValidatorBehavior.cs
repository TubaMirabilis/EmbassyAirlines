using EaCommon.Errors;
using EaCommon.Interfaces;
using FluentResults;
using Mediator;

namespace EaCommon.PipelineBehaviors;

public sealed class MessageValidatorBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IValidate
    where TResponse : ResultBase, new()
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken ct,
        MessageHandlerDelegate<TMessage, TResponse> next)
    {
        var validationResult = message.Validate();
        if (validationResult.IsSuccess)
        {
            return await next(message, ct);
        }
        var result = new TResponse();
        foreach (var reason in validationResult.Reasons)
        {
            result.Errors.Add(new ValidationError(reason.Message));
        }
        return result;
    }
}