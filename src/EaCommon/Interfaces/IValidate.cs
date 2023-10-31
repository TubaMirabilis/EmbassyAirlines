using FluentValidation.Results;
using Mediator;

namespace EaCommon.Interfaces;

public interface IValidate : IMessage
{
    Task<ValidationResult> ValidateAsync(CancellationToken ct);
}