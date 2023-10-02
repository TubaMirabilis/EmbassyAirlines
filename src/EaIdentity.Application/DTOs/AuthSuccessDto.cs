namespace EaIdentity.Application.Dtos;

public sealed record AuthSuccessDto(string Token, string RefreshToken);