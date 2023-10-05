namespace EaIdentity.Infrastructure.Options;

internal sealed class JwtSettings
{
    public required string Secret { get; set; }
    public required TimeSpan TokenLifetime { get; set; }
}