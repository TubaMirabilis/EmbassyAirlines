using Flights.Api.Domain.Bookings;
using Shared.Contracts;

namespace Flights.Api.Extensions;

internal static class BookingExtensions
{
    public static BookingDto ToDto(this Booking booking)
    {
        var passengers = booking.Passengers;
        var seats = booking.GetSeats()
                           .ToList();
        var details = new Dictionary<Guid, KeyValuePair<PassengerDto, SeatDto>>();
        for (var i = 0; i < passengers.Count; i++)
        {
            var passenger = passengers[i];
            var seat = seats[i];
            details.Add(passenger.Id, new KeyValuePair<PassengerDto, SeatDto>(passenger.ToDto(), seat.ToDto()));
        }
        return new BookingDto(booking.Flight.FlightNumber, details);
    }
}
