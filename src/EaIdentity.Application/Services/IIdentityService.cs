using EaIdentity.Domain;

namespace EaIdentity.Application.Services;

public interface IIdentityService
{
    Task<AuthenticationResult> RegisterAsync(string email, string password, CancellationToken ct);
    Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken ct);
    Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken, CancellationToken ct);
}