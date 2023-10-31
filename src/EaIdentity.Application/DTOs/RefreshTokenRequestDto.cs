using EaIdentity.Domain;
using ErrorOr;
using Mediator;

namespace EaIdentity.Application.Dtos;

public sealed record RefreshTokenRequestDto(string Token, string RefreshToken)
    : ICommand<ErrorOr<AuthenticationResult>>;