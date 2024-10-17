using System.Globalization;
using System.Text.Json;
using Flights.Api.Database;
using Flights.Api.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Shared.Contracts;
using TechTalk.SpecFlow;

namespace Flights.Api.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class SearchForFlightsByRouteAndDateSteps : IDisposable
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private readonly IServiceScope _scope;

    public SearchForFlightsByRouteAndDateSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
    }

    [Given(@"the following flights exist:")]
    public async Task GivenTheFollowingFlightsExist(Table table)
    {
        var flights = ParseFlightsFromTable(table);
        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Flights.AddRange(flights);
        await dbContext.SaveChangesAsync();
    }

    [When(@"I search for flights from (.*) to (.*) on (.*)")]
    public async Task WhenISearchForFlightsFromToOn(string departure, string arrival, string date)
    {
        var url = $"/flights?departure={departure}&arrival={arrival}&date={date}";
        _response = await _client.GetAsync(url);
    }

    [Then(@"the following flights are returned:")]
    public async Task ThenTheFollowingFlightsAreReturned(Table table)
    {
        var expectedFlights = GetExpectedFlightsFromTable(table);
        var actualFlights = await GetFlightsFromResponse();
        actualFlights.Should().BeEquivalentTo(expectedFlights);
    }

    [Then(@"no flights are returned")]
    public async Task ThenNoFlightsAreReturned()
    {
        var actualFlights = await GetFlightsFromResponse();
        actualFlights.Should().BeEmpty();
    }

    private static IEnumerable<Flight> ParseFlightsFromTable(Table table)
    {
        foreach (var row in table.Rows)
        {
            yield return CreateFlightFromRow(row);
        }
    }

    private static Flight CreateFlightFromRow(TableRow row)
    {
        var flightNumber = row["FlightNumber"];
        var departure = row["DepartureAirport"];
        var destination = row["ArrivalAirport"];
        var departureTime = DateTimeOffset.Parse(row["DepartureTime"], CultureInfo.InvariantCulture);
        var arrivalTime = DateTimeOffset.Parse(row["ArrivalTime"], CultureInfo.InvariantCulture);
        var economyPrice = decimal.Parse(row["EconomyPrice"], CultureInfo.InvariantCulture);
        var businessPrice = decimal.Parse(row["BusinessPrice"], CultureInfo.InvariantCulture);
        var availableEconomySeats = int.Parse(row["AvailableEconomySeats"], CultureInfo.InvariantCulture);
        var availableBusinessSeats = int.Parse(row["AvailableBusinessSeats"], CultureInfo.InvariantCulture);
        var departureInstant = Instant.FromDateTimeOffset(departureTime);
        var arrivalInstant = Instant.FromDateTimeOffset(arrivalTime);
        var departureZdt = new ZonedDateTime(departureInstant, DateTimeZoneProviders.Tzdb["America/Vancouver"]).WithZone(DateTimeZone.Utc);
        var arrivalZdt = new ZonedDateTime(arrivalInstant, DateTimeZoneProviders.Tzdb["Europe/Paris"]).WithZone(DateTimeZone.Utc);
        var schedule = new FlightSchedule(departure, destination, departureZdt, arrivalZdt);
        var pricing = new FlightPricing(economyPrice, businessPrice);
        var availableSeats = new AvailableSeats(availableEconomySeats, availableBusinessSeats);
        return Flight.Create(flightNumber, schedule, pricing, availableSeats);
    }

    private static IEnumerable<object> GetExpectedFlightsFromTable(Table table) => table.Rows.Select(row => new
    {
        FlightNumber = row["FlightNumber"],
        Departure = row["DepartureAirport"],
        Destination = row["ArrivalAirport"],
        DepartureTime = DateTimeOffset.Parse(row["DepartureTime"], CultureInfo.InvariantCulture),
        ArrivalTime = DateTimeOffset.Parse(row["ArrivalTime"], CultureInfo.InvariantCulture),
        EconomyPrice = decimal.Parse(row["EconomyPrice"], CultureInfo.InvariantCulture),
        BusinessPrice = decimal.Parse(row["BusinessPrice"], CultureInfo.InvariantCulture),
        AvailableEconomySeats = int.Parse(row["AvailableEconomySeats"], CultureInfo.InvariantCulture),
        AvailableBusinessSeats = int.Parse(row["AvailableBusinessSeats"], CultureInfo.InvariantCulture)
    });

    private async Task<IEnumerable<object>> GetFlightsFromResponse()
    {
        ArgumentNullException.ThrowIfNull(_response);
        _response.EnsureSuccessStatusCode();
        var content = await _response.Content.ReadAsStreamAsync();
        var options = _scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        var flights = await JsonSerializer.DeserializeAsync<IEnumerable<FlightDto>>(content, options);
        ArgumentNullException.ThrowIfNull(flights);
        return flights.Select(f => new
        {
            f.FlightNumber,
            f.Departure,
            f.Destination,
            f.DepartureTime,
            f.ArrivalTime,
            f.EconomyPrice,
            f.BusinessPrice,
            f.AvailableEconomySeats,
            f.AvailableBusinessSeats
        });
    }

    public void Dispose()
    {
        _response?.Dispose();
        _scope.Dispose();
        _client.Dispose();
    }
}
