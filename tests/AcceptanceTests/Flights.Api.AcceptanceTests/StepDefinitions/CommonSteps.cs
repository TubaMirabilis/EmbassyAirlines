using System.Globalization;
using Flights.Api.Database;
using Flights.Api.Domain.Flights;
using Flights.Api.Domain.Seats;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using TechTalk.SpecFlow;

namespace Flights.Api.AcceptanceTests.StepDefinitions;

[Binding]
internal sealed class CommonSteps : IDisposable
{
    private readonly IServiceScope _scope;
    public CommonSteps(WebApplicationFactory<Program> factory)
    {
        _scope = factory.Services
                        .CreateScope();
    }
    public void Dispose() => _scope.Dispose();
    [Given("the following flights exist:")]
    public async Task GivenTheFollowingFlightsExist(Table table)
    {
        var flights = table.Rows
                           .Select(ParseFlight);
        await using var dbContext = _scope.ServiceProvider
                                          .GetRequiredService<ApplicationDbContext>();
        dbContext.Flights
                 .AddRange(flights);
        await dbContext.SaveChangesAsync();
    }
    private Flight ParseFlight(TableRow row)
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
        var seatService = _scope.ServiceProvider
                                .GetRequiredService<ISeatService>();
        var seats = seatService.CreateSeats("B78X", economyPrice, businessPrice);
        var departureAirport = new Airport(departureAirportIataCode, departureAirportTimeZone);
        var destinationAirport = new Airport(destinationAirportIataCode, destinationAirportTimeZone);
        var departureInstant = Instant.FromDateTimeOffset(departureTime);
        var arrivalInstant = Instant.FromDateTimeOffset(arrivalTime);
        var departureDtz = DateTimeZoneProviders.Tzdb[departureAirportTimeZone];
        var destinationDtz = DateTimeZoneProviders.Tzdb[destinationAirportTimeZone];
        var utc = DateTimeZone.Utc;
        var departureZdt = new ZonedDateTime(departureInstant, departureDtz).WithZone(utc);
        var arrivalZdt = new ZonedDateTime(arrivalInstant, destinationDtz).WithZone(utc);
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
