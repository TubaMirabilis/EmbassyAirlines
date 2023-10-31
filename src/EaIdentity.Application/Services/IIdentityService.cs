using EaIdentity.Domain;
using ErrorOr;

namespace EaIdentity.Application.Services;

public interface IIdentityService
{
    Task<ErrorOr<AuthenticationResult>> RegisterAsync(string email, string password, CancellationToken ct);
    Task<ErrorOr<AuthenticationResult>> LoginAsync(string email, string password, CancellationToken ct);
    Task<ErrorOr<AuthenticationResult>> RefreshTokenAsync(string token, string refreshToken, CancellationToken ct);
}