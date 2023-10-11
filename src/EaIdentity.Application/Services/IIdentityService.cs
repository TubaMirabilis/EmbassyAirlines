using EaIdentity.Domain;
using FluentResults;

namespace EaIdentity.Application.Services;

public interface IIdentityService
{
    Task<Result<AuthenticationResult>> RegisterAsync(string email, string password, CancellationToken ct);
    Task<Result<AuthenticationResult>> LoginAsync(string email, string password, CancellationToken ct);
    Task<Result<AuthenticationResult>> RefreshTokenAsync(string token, string refreshToken, CancellationToken ct);
}