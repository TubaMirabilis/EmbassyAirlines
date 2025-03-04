using Flights.Api.Domain.Seats;
using Shared.Contracts;

namespace Flights.Api.Extensions;

internal static class SeatExtensions
{
    public static SeatDto ToDto(this Seat seat)
        => new(
            seat.Id,
            seat.SeatNumber,
            seat.SeatType.ToString(),
            seat.IsBooked,
            seat.Price
        );
}
