using Microsoft.AspNetCore.Identity;

namespace EaIdentity.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public List<RefreshToken> RefreshTokens { get; } = new();
}