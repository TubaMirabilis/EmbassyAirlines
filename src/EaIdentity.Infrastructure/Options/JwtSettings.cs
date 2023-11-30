namespace EaIdentity.Infrastructure.Options;

internal sealed record JwtSettings(string Secret, TimeSpan TokenLifetime, string Issuer, string Audience);