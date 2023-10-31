using EaIdentity.Domain;
using ErrorOr;
using Mediator;

namespace EaIdentity.Application.Dtos;

public sealed record UserLoginRequestDto(string Email, string Password)
    : ICommand<ErrorOr<AuthenticationResult>>;