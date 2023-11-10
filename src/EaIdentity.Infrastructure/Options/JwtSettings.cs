namespace EaIdentity.Infrastructure.Options;

internal sealed class JwtSettings
{
    public required string Secret { get; set; }
    public required TimeSpan TokenLifetime { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
}