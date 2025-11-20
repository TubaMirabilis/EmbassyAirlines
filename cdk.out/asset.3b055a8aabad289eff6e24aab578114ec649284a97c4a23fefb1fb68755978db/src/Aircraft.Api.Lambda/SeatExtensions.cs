using Shared.Contracts;

namespace Aircraft.Api.Lambda;

internal static class SeatExtensions
{
    public static SeatDto ToDto(this Seat seat) => new(
        seat.Id,
        seat.AircraftId,
        seat.RowNumber,
        seat.Letter,
        seat.Type.ToString()
    );
}
