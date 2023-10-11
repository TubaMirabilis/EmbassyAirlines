using EaIdentity.Domain;
using FluentResults;
using Mediator;

namespace EaIdentity.Application.Dtos;

public sealed record UserLoginRequestDto(string Email, string Password)
    : ICommand<Result<AuthenticationResult>>;