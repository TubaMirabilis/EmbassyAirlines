using EaCommon.Interfaces;
using EaIdentity.Application.Validators;
using EaIdentity.Domain;
using ErrorOr;
using FluentValidation.Results;
using Mediator;

namespace EaIdentity.Application.Dtos;

public sealed record UserRegistrationDto(string Email, string Password)
    : ICommand<ErrorOr<AuthenticationResult>>, IValidate
{
    public async Task<ValidationResult> ValidateAsync(CancellationToken ct)
        => await new UserRegistrationDtoValidator().ValidateAsync(this, ct);
}