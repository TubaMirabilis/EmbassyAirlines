using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Journeys;

public class SearchForJourneysByRouteAndDateTests : BaseFunctionalTest
{
    public SearchForJourneysByRouteAndDateTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDepartureIsMissing()
    {
        // Arrange
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var uri = new Uri("journeys?departure=&destination=CDG&date=2022-01-01", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDestinationIsMissing()
    {
        // Arrange
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var uri = new Uri("journeys?departure=CDG&destination=&date=2022-01-01", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDateIsMissing()
    {
        // Arrange
        var error = "Date is required.";

        // Act
        var uri = new Uri("journeys?departure=CDG&destination=JFK&date=", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDateIsInWrongFormat()
    {
        // Arrange
        var error = "Invalid date format. Please use yyyy-MM-dd.";

        // Act
        var uri = new Uri("journeys?departure=CDG&destination=JFK&date=2022-01-32", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDepartureAndDestinationAreTheSame()
    {
        // Arrange
        var error = "Destination cannot be the same as departure.";

        // Act
        var uri = new Uri("journeys?departure=CDG&destination=CDG&date=2022-01-01", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenDirectFlightExists()
    {
        // Arrange
        var sin = await SeedAirportAsync(new CreateAirportDto("SIN", "Singapore Changi Airport", "Asia/Singapore"));
        var maa = await SeedAirportAsync(new CreateAirportDto("MAA", "Chennai International Airport", "Asia/Kolkata"));
        var now = DateTime.Now;
        var date = now.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var flightRequest = new ScheduleFlightDto("EX253", sin.Id, now.AddDays(1), maa.Id, now.AddDays(1).AddHours(1).AddMinutes(35), 1000, 2000, "B78X");
        var flightResult = await SeedFlightAsync(flightRequest);

        // Act
        var uri = new Uri($"journeys?departure={sin.IataCode}&destination={maa.IataCode}&date={date}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var journeyListWrapper = await response.Content.ReadFromJsonAsync<JourneyListDto>();
        ArgumentNullException.ThrowIfNull(journeyListWrapper);
        journeyListWrapper.Journeys.First().Should().BeEquivalentTo(new FlightDto[] { flightResult });
    }
}
