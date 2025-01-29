using Flights.Api.Domain.Bookings;
using Shared.Contracts;

namespace Flights.Api.Extensions;

internal static class BookingExtensions
{
    public static BookingDto ToDto(this Booking booking) => new(
        booking.Id,
        booking.Reference,
        booking.Seats.Select(s => s.ToDto()).ToList(),
        booking.Passengers.Select(p => p.ToDto()).ToList());
}
