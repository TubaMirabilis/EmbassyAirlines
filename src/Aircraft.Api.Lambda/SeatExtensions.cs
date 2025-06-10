namespace Aircraft.Api.Lambda;

public static class SeatExtensions
{
    public static SeatDto ToDto(this Seat seat) => new(
        seat.Id,
        seat.AircraftId,
        seat.RowNumber,
        seat.Letter,
        seat.Type.ToString()
    );
}
