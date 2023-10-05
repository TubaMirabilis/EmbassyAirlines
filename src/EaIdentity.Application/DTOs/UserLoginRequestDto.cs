using EaIdentity.Domain;
using Mediator;

namespace EaIdentity.Application.Dtos;

public sealed record UserLoginRequestDto(string Email, string Password)
    : ICommand<AuthenticationResult>;