namespace Aircraft.Api.Lambda;

public sealed record SeatDto(Guid Id, Guid AircraftId, byte RowNumber, char Letter, string Type);
