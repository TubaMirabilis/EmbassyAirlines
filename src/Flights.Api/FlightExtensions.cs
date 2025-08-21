using System.Globalization;
using Shared.Contracts;

namespace Flights.Api;

internal static class FlightExtensions
{
    public static FlightDto ToDto(this Flight flight) => new(
        flight.Id,
        flight.FlightNumberIata,
        flight.FlightNumberIcao,
        flight.DepartureAirport.Id,
        flight.DepartureAirport.IataCode,
        flight.DepartureAirport.IcaoCode,
        flight.DepartureAirport.Name,
        flight.DepartureAirport.TimeZoneId,
        flight.ArrivalAirport.Id,
        flight.ArrivalAirport.IataCode,
        flight.ArrivalAirport.IcaoCode,
        flight.ArrivalAirport.Name,
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
