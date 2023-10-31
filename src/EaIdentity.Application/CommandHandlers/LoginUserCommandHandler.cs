using EaIdentity.Application.Dtos;
using EaIdentity.Application.Services;
using EaIdentity.Domain;
using ErrorOr;
using Mediator;

namespace EaIdentity.Application.CommandHandlers;

public sealed class LoginUserCommandHandler : ICommandHandler<UserLoginRequestDto, ErrorOr<AuthenticationResult>>
{
    private readonly IIdentityService _identityService;
    public LoginUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }
    public async ValueTask<ErrorOr<AuthenticationResult>> Handle(UserLoginRequestDto command, CancellationToken ct)
        => await _identityService.LoginAsync(command.Email, command.Password, ct);
}