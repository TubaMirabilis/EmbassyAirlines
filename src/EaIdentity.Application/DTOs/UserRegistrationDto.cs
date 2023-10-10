using EaCommon.Errors;
using EaCommon.Interfaces;
using EaIdentity.Application.Validators;
using EaIdentity.Domain;
using Mediator;
using System.Diagnostics.CodeAnalysis;

namespace EaIdentity.Application.Dtos;

public sealed record UserRegistrationDto(string Email, string Password)
    : ICommand<AuthenticationResult>, IValidate
{
    public bool IsValid([NotNullWhen(false)] out ValidationError? error)
    {
        var validator = new UserRegistrationDtoValidator();
        var result = validator.Validate(this);
        if (result.IsValid)
        {
            error = null;
            return result.IsValid;
        }
        error = new ValidationError(result.Errors.Select(e => e.ErrorMessage)
            .ToArray());
        return !result.IsValid;
    }
}