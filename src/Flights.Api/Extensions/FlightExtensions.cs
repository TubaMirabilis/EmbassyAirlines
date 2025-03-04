using System.Globalization;
using Flights.Api.Domain.Flights;
using Flights.Api.Domain.Seats;
using Shared.Contracts;

namespace Flights.Api.Extensions;

internal static class FlightExtensions
{
    public static FlightDto ToDto(this Flight flight) => new(
        flight.Id,
        flight.FlightNumber,
        flight.DepartureAirport.IataCode,
        flight.ArrivalAirport.IataCode,
        flight.ScheduledDeparture.ToDateTimeOffset().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        flight.ScheduledArrival.ToDateTimeOffset().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        flight.ArrivalInstant.Minus(flight.DepartureInstant).ToTimeSpan(),
        flight.CheapestEconomyPrice,
        flight.CheapestBusinessPrice,
        flight.Seats.Count(s => s is { IsBooked: false, SeatType: SeatType.Economy }),
        flight.Seats.Count(s => s is { IsBooked: false, SeatType: SeatType.Business }));
}
