using EaIdentity.Domain;
using FluentResults;
using Mediator;

namespace EaIdentity.Application.Dtos;

public sealed record RefreshTokenRequestDto(string Token, string RefreshToken)
    : ICommand<Result<AuthenticationResult>>;