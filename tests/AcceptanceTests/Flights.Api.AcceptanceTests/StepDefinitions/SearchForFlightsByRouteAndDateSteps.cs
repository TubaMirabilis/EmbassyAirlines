using System.Globalization;
using System.Text.Json;
using Flights.Api.Database;
using Flights.Api.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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
    private readonly JsonSerializerOptions _options;
    public SearchForFlightsByRouteAndDateSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        _options = _scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
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
    public async Task WhenISearchForFlightsFromToOn(string departure, string destination, string date)
    {
        var url = $"/flights?departure={departure}&destination={destination}&date={date}";
        _response = await _client.GetAsync(url);
    }

    [When(@"I search for flights from (.*) to (.*) on")]
    public async Task WhenISearchForFlightsFromToOn(string departure, string destination)
    {
        var url = $"/flights?departure={departure}&destination={destination}";
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

    [Then(@"an error message is returned which states that the departure and destination airports cannot be the same")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDepartureAndDestinationAirportsCannotBeTheSame() => await GetProblemDetailsFromResponseAndAssert("Destination cannot be the same as departure");

    [Then(@"an error message is returned which states that the destination airport code is required")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDestinationAirportCodeIsRequired() => await GetProblemDetailsFromResponseAndAssert("Destination is required");

    [Then(@"an error message is returned which states that the departure airport code is required")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDepartureAirportCodeIsRequired() => await GetProblemDetailsFromResponseAndAssert("Departure is required");

    [Then(@"an error message is returned which states that the date format is invalid")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDateFormatIsInvalid() => await GetProblemDetailsFromResponseAndAssert("Invalid date format. Please use yyyy-MM-dd");

    [Then(@"an error message is returned which states that the date parameter is required")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDateParameterIsRequired() => await GetProblemDetailsFromResponseAndAssert("Date is required");

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
        var destination = row["DestinationAirport"];
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
        Destination = row["DestinationAirport"],
        DepartureTime = DateTimeOffset.Parse(row["DepartureTime"], CultureInfo.InvariantCulture),
        ArrivalTime = DateTimeOffset.Parse(row["ArrivalTime"], CultureInfo.InvariantCulture),
        EconomyPrice = decimal.Parse(row["EconomyPrice"], CultureInfo.InvariantCulture),
        BusinessPrice = decimal.Parse(row["BusinessPrice"], CultureInfo.InvariantCulture),
        AvailableEconomySeats = int.Parse(row["AvailableEconomySeats"], CultureInfo.InvariantCulture),
        AvailableBusinessSeats = int.Parse(row["AvailableBusinessSeats"], CultureInfo.InvariantCulture)
    });

    private async Task GetProblemDetailsFromResponseAndAssert(string detail)
    {
        ArgumentNullException.ThrowIfNull(_response);
        var expectedProblemDetails = CreateProblemDetails(detail);
        var content = await _response.Content.ReadAsStreamAsync();
        var actualProblemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(content, _options);
        actualProblemDetails.Should().BeEquivalentTo(expectedProblemDetails);
    }

    private async Task<IEnumerable<object>> GetFlightsFromResponse()
    {
        ArgumentNullException.ThrowIfNull(_response);
        _response.EnsureSuccessStatusCode();
        var content = await _response.Content.ReadAsStreamAsync();
        var flights = await JsonSerializer.DeserializeAsync<IEnumerable<FlightDto>>(content, _options);
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

    private static ProblemDetails CreateProblemDetails(string detail) => new ProblemDetails
    {
        Title = "Validation Error",
        Status = 400,
        Detail = detail,
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
    };

    public void Dispose()
    {
        _response?.Dispose();
        _scope.Dispose();
        _client.Dispose();
    }
}