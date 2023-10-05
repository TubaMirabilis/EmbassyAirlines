using EaIdentity.Application.Dtos;
using EaIdentity.Application.Services;
using EaIdentity.Domain;
using Mediator;

namespace EaIdentity.Application.CommandHandlers;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenRequestDto, AuthenticationResult>
{
    private readonly IIdentityService _identityService;
    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }
    public async ValueTask<AuthenticationResult> Handle(RefreshTokenRequestDto command, CancellationToken ct)
        => await _identityService.RefreshTokenAsync(command.Token, command.RefreshToken, ct);
}