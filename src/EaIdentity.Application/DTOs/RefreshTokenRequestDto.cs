using EaIdentity.Domain;
using Mediator;

namespace EaIdentity.Application.Dtos;

public sealed record RefreshTokenRequestDto(string Token, string RefreshToken) : ICommand<AuthenticationResult>;