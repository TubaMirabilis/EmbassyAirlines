namespace EaIdentity.Application.Dtos;

public sealed record AuthenticationResult(string Token, string RefreshToken, bool Success, IEnumerable<string> Errors);