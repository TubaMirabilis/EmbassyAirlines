namespace Aircraft.Core.Models;

public sealed class Seat
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid AircraftId { get; init; }
    public byte RowNumber { get; init; }
    public char Letter { get; init; }
    public SeatType Type { get; init; }
}
