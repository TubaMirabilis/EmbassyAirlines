using System.Globalization;
using Shared.Contracts;

namespace Flights.Api;

internal static class FlightExtensions
{
    public static FlightDto ToDto(this Flight flight) => new(
        flight.Id,
        flight.FlightNumber,
        flight.DepartureAirport.Id,
        flight.DepartureAirport.IataCode,
        flight.DepartureAirport.TimeZoneId,
        flight.ArrivalAirport.Id,
        flight.ArrivalAirport.IataCode,
        flight.ArrivalAirport.TimeZoneId,
        flight.DepartureZonedTime.ToDateTimeOffset().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        flight.ArrivalZonedTime.ToDateTimeOffset().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        flight.ArrivalInstant.Minus(flight.DepartureInstant).ToTimeSpan(),
        flight.EconomyPrice.Amount,
        flight.BusinessPrice.Amount,
        flight.Aircraft.Id,
        flight.Aircraft.EquipmentCode,
        flight.Aircraft.TailNumber);
}
