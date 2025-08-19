namespace Shared.Contracts;

public sealed record SeatDto(Guid Id, Guid AircraftId, byte RowNumber, char Letter, string Type);
