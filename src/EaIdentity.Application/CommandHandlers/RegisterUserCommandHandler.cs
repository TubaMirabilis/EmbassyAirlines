using EaIdentity.Application.Dtos;
using EaIdentity.Application.Services;
using EaIdentity.Domain;
using Mediator;

namespace EaIdentity.Application.CommandHandlers;

public sealed class RegisterUserCommandHandler : ICommandHandler<UserRegistrationDto, AuthenticationResult>
{
    private readonly IIdentityService _identityService;
    public RegisterUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }
    public async ValueTask<AuthenticationResult> Handle(UserRegistrationDto command, CancellationToken ct)
        => await _identityService.RegisterAsync(command.Email, command.Password, ct);
}