using System.Globalization;
using System.Text.Json;
using Flights.Api.AcceptanceTests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using TechTalk.SpecFlow;

namespace Flights.Api.AcceptanceTests.StepDefinitions;

[Binding]
internal sealed class SearchForFlightsByRouteAndDateSteps : IDisposable
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private readonly IServiceScope _scope;
    private readonly JsonSerializerOptions _options;
    public SearchForFlightsByRouteAndDateSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _scope = factory.Services
                        .CreateScope();
        _options = _scope.ServiceProvider
                         .GetRequiredService<JsonSerializerOptions>();
    }

    [When("I search for flights from (.*) to (.*) on (.*)")]
    public async Task WhenISearchForFlightsFromToOn(string departure, string destination, string date)
    {
        var baseAddress = _client.BaseAddress ?? throw new InvalidOperationException("Base address is null");
        var uri = new UriBuilder(baseAddress)
        {
            Path = "/flights",
            Query = $"departure={departure}&destination={destination}&date={date}"
        }.Uri;
        _response = await _client.GetAsync(uri);
    }

    [When("I search for flights from (.*) to (.*) on")]
    public async Task WhenISearchForFlightsFromToOn(string departure, string destination)
    {
        var baseAddress = _client.BaseAddress ?? throw new InvalidOperationException("Base address is null");
        var uri = new UriBuilder(baseAddress)
        {
            Path = "/flights",
            Query = $"departure={departure}&destination={destination}"
        }.Uri;
        _response = await _client.GetAsync(uri);
    }

    [Then("the following flights are returned:")]
    public async Task ThenTheFollowingFlightsAreReturned(Table table)
    {
        var expectedFlights = GetExpectedFlightsFromTable(table);
        var actualFlights = await GetFlightsFromResponse();
        actualFlights.Should()
                     .BeEquivalentTo(expectedFlights);
    }

    [Then("no flights are returned")]
    public async Task ThenNoFlightsAreReturned()
    {
        var actualFlights = await GetFlightsFromResponse();
        actualFlights.Should()
                     .BeEmpty();
    }

    [Then("an error message is returned which states that the departure and destination airports cannot be the same")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDepartureAndDestinationAirportsCannotBeTheSame()
        => await GetProblemDetailsFromResponseAndAssert("Destination cannot be the same as departure");

    [Then("an error message is returned which states that the destination airport code is required")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDestinationAirportCodeIsRequired()
        => await GetProblemDetailsFromResponseAndAssert("Destination is required");

    [Then("an error message is returned which states that the departure airport code is required")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDepartureAirportCodeIsRequired()
        => await GetProblemDetailsFromResponseAndAssert("Departure is required");

    [Then("an error message is returned which states that the date format is invalid")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDateFormatIsInvalid()
        => await GetProblemDetailsFromResponseAndAssert("Invalid date format. Please use yyyy-MM-dd");

    [Then("an error message is returned which states that the date parameter is required")]
    public async Task ThenAnErrorMessageIsReturnedWhichStatesThatTheDateParameterIsRequired()
        => await GetProblemDetailsFromResponseAndAssert("Date is required");

    private static IEnumerable<FlightDto> GetExpectedFlightsFromTable(Table table) => table.Rows.Select(row => new FlightDto
    (
        row["FlightNumber"],
        row["DepartureAirportIataCode"],
        row["DestinationAirportIataCode"],
        DateTimeOffset.Parse(row["DepartureTime"], CultureInfo.InvariantCulture),
        DateTimeOffset.Parse(row["ArrivalTime"], CultureInfo.InvariantCulture),
        decimal.Parse(row["CheapestEconomyPrice"], CultureInfo.InvariantCulture),
        decimal.Parse(row["CheapestBusinessPrice"], CultureInfo.InvariantCulture),
        int.Parse(row["AvailableEconomySeats"], CultureInfo.InvariantCulture),
        int.Parse(row["AvailableBusinessSeats"], CultureInfo.InvariantCulture)
    ));

    private async Task GetProblemDetailsFromResponseAndAssert(string detail)
    {
        ArgumentNullException.ThrowIfNull(_response);
        var expectedProblemDetails = new ProblemDetails().WithValidationError(detail);
        var content = await _response.Content
                                     .ReadAsStreamAsync();
        var actualProblemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(content, _options);
        actualProblemDetails.Should()
                            .BeEquivalentTo(expectedProblemDetails, options => options.Excluding(p => p.Extensions));
    }

    private async Task<IEnumerable<FlightDto>> GetFlightsFromResponse()
    {
        ArgumentNullException.ThrowIfNull(_response);
        _response.EnsureSuccessStatusCode();
        var content = await _response.Content
                                     .ReadAsStreamAsync();
        var flights = await JsonSerializer.DeserializeAsync<IEnumerable<FlightDto>>(content, _options);
        ArgumentNullException.ThrowIfNull(flights);
        return flights;
    }

    public void Dispose()
    {
        _response?.Dispose();
        _scope.Dispose();
        _client.Dispose();
    }
}
