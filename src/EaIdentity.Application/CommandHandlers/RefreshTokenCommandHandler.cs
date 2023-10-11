using EaIdentity.Application.Dtos;
using EaIdentity.Application.Services;
using EaIdentity.Domain;
using FluentResults;
using Mediator;

namespace EaIdentity.Application.CommandHandlers;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenRequestDto, Result<AuthenticationResult>>
{
    private readonly IIdentityService _identityService;
    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }
    public async ValueTask<Result<AuthenticationResult>> Handle(RefreshTokenRequestDto command, CancellationToken ct)
        => await _identityService.RefreshTokenAsync(command.Token, command.RefreshToken, ct);
}