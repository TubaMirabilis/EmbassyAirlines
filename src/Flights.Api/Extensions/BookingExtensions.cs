using Flights.Api.Domain.Bookings;
using Shared.Contracts;

namespace Flights.Api.Extensions;

internal static class BookingExtensions
{
    public static BookingDto ToDto(this Booking booking) => new(
        booking.Id,
        booking.Reference,
        booking.Seat.FlightId,
        booking.Seat.SeatNumber,
        booking.PassengerName,
        booking.PassengerEmail);
}
