namespace EaIdentity.Application.Dtos;

public sealed record AuthFailedDto(IEnumerable<string> Errors);