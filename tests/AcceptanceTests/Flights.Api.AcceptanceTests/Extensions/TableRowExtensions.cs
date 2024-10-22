using System.Globalization;
using Flights.Api.Domain.Flights;
using Flights.Api.Services;
using NodaTime;
using TechTalk.SpecFlow;

namespace Flights.Api.AcceptanceTests.Extensions;

public static class TableRowExtensions
{
    public static Flight ParseFlight(this TableRow row)
    {
        var flightNumber = row["FlightNumber"];
        var departureAirportIataCode = row["DepartureAirportIataCode"];
        var departureAirportTimeZone = row["DepartureAirportTimeZone"];
        var destinationAirportIataCode = row["DestinationAirportIataCode"];
        var destinationAirportTimeZone = row["DestinationAirportTimeZone"];
        var departureTime = DateTimeOffset.Parse(row["DepartureTime"], CultureInfo.InvariantCulture);
        var arrivalTime = DateTimeOffset.Parse(row["ArrivalTime"], CultureInfo.InvariantCulture);
        var economyPrice = decimal.Parse(row["EconomyPrice"], CultureInfo.InvariantCulture);
        var businessPrice = decimal.Parse(row["BusinessPrice"], CultureInfo.InvariantCulture);
        var seats = new SeatService().CreateSeats("B78X", economyPrice, businessPrice);
        var departureAirport = new Airport(departureAirportIataCode, departureAirportTimeZone);
        var destinationAirport = new Airport(destinationAirportIataCode, destinationAirportTimeZone);
        var departureInstant = Instant.FromDateTimeOffset(departureTime);
        var arrivalInstant = Instant.FromDateTimeOffset(arrivalTime);
        var departureDtz = DateTimeZoneProviders.Tzdb[departureAirportTimeZone];
        var destinationDtz = DateTimeZoneProviders.Tzdb[destinationAirportTimeZone];
        var departureZdt = new ZonedDateTime(departureInstant, departureDtz).WithZone(DateTimeZone.Utc);
        var arrivalZdt = new ZonedDateTime(arrivalInstant, destinationDtz).WithZone(DateTimeZone.Utc);
        var schedule = new FlightSchedule
        {
            DepartureAirport = departureAirport,
            DestinationAirport = destinationAirport,
            DepartureTime = departureZdt,
            ArrivalTime = arrivalZdt
        };
        return Flight.Create(flightNumber, schedule, seats);
    }
}
