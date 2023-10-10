using EaCommon.Interfaces;
using EaIdentity.Application.Validators;
using EaIdentity.Domain;
using FluentResults;
using Mediator;

namespace EaIdentity.Application.Dtos;

public sealed record UserRegistrationDto(string Email, string Password)
    : ICommand<AuthenticationResult>, IValidate
{
    public Result Validate()
    {
        var validator = new UserRegistrationDtoValidator();
        var result = validator.Validate(this);
        if (result.IsValid)
        {
            return Result.Ok();
        }
        return Result.Fail(result.Errors.Select(e => e.ErrorMessage));
    }
}