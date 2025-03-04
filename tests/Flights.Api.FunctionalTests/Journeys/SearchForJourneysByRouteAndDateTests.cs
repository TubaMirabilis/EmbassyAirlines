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
    public async Task Should_ReturnNotFound_WhenDepartureAirportDoesNotExist()
    {
        // Arrange
        var error = "Airport with IATA code CDG not found.";

        // Act
        var uri = new Uri("journeys?departure=CDG&destination=ICN&date=2022-01-01", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDateIsInThePast()
    {
        // Arrange
        await SeedAirportAsync(new CreateAirportDto("EMA", "East Midlands Airport", "Europe/London"));
        var error = "Departure date cannot be in the past.";

        // Act
        var uri = new Uri("journeys?departure=EMA&destination=ICN&date=2022-01-01", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_WhenDepartureAirportHasNoFlights()
    {
        // Arrange
        var now = DateTime.Now;
        var date = now.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await SeedAirportAsync(new CreateAirportDto("NCL", "Newcastle Airport", "Europe/London"));

        // Act
        var uri = new Uri($"journeys?departure=NCL&destination=IST&date={date}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var journeyListWrapper = await response.Content.ReadFromJsonAsync<JourneyListDto>();
        ArgumentNullException.ThrowIfNull(journeyListWrapper);
        journeyListWrapper.Journeys.Should().BeEmpty();
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
        journeyListWrapper.Journeys.First().Should().BeEquivalentTo([flightResult]);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenJourneyWithTwoFlightsExists()
    {
        // Arrange
        var dxb = await SeedAirportAsync(new CreateAirportDto("DXB", "Dubai International Airport", "Asia/Dubai"));
        var ist = await SeedAirportAsync(new CreateAirportDto("IST", "Istanbul Airport", "Europe/Istanbul"));
        var ams = await SeedAirportAsync(new CreateAirportDto("AMS", "Amsterdam Airport Schiphol", "Europe/Amsterdam"));
        var now = DateTime.Now;
        var date = now.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var firstFlightRequest = new ScheduleFlightDto("EX254", dxb.Id, now.AddDays(1), ist.Id, now.AddDays(1).AddHours(4).AddMinutes(10), 1000, 2000, "B78X");
        var firstFlightResult = await SeedFlightAsync(firstFlightRequest);
        var secondFlightRequest = new ScheduleFlightDto("EX255", ist.Id, now.AddDays(1).AddHours(5).AddMinutes(10),
            ams.Id, now.AddDays(1).AddHours(6).AddMinutes(55), 1000, 2000, "B78X");
        var secondFlightResult = await SeedFlightAsync(secondFlightRequest);

        // Act
        var uri = new Uri($"journeys?departure={dxb.IataCode}&destination={ams.IataCode}&date={date}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var journeyListWrapper = await response.Content.ReadFromJsonAsync<JourneyListDto>();
        ArgumentNullException.ThrowIfNull(journeyListWrapper);
        journeyListWrapper.Journeys.First().Should().BeEquivalentTo([firstFlightResult, secondFlightResult]);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenJourneyWithThreeFlightsExists()
    {
        // Arrange
        var muc = await SeedAirportAsync(new CreateAirportDto("MUC", "Munich Airport", "Europe/Berlin"));
        var tlv = await SeedAirportAsync(new CreateAirportDto("TLV", "Ben Gurion Airport", "Asia/Jerusalem"));
        var auh = await SeedAirportAsync(new CreateAirportDto("AUH", "Abu Dhabi International Airport", "Asia/Dubai"));
        var bkk = await SeedAirportAsync(new CreateAirportDto("BKK", "Suvarnabhumi Airport", "Asia/Bangkok"));
        var now = DateTime.Now;
        var date = now.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var firstFlightRequest = new ScheduleFlightDto("EX354", muc.Id, now.AddDays(1), tlv.Id, now.AddDays(1).AddHours(4).AddMinutes(45), 1000, 2000, "B78X");
        var firstFlightResult = await SeedFlightAsync(firstFlightRequest);
        var secondFlightRequest = new ScheduleFlightDto("EX355", tlv.Id, now.AddDays(1).AddHours(5).AddMinutes(45),
            auh.Id, now.AddDays(1).AddHours(10).AddMinutes(45), 1000, 2000, "B78X");
        var secondFlightResult = await SeedFlightAsync(secondFlightRequest);
        var thirdFlightRequest = new ScheduleFlightDto("EX385", auh.Id, now.AddDays(1).AddHours(11).AddMinutes(45),
            bkk.Id, now.AddDays(1).AddHours(20).AddMinutes(50), 1000, 2000, "B78X");
        var thirdFlightResult = await SeedFlightAsync(thirdFlightRequest);

        // Act
        var uri = new Uri($"journeys?departure={muc.IataCode}&destination={bkk.IataCode}&date={date}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var journeyListWrapper = await response.Content.ReadFromJsonAsync<JourneyListDto>();
        ArgumentNullException.ThrowIfNull(journeyListWrapper);
        journeyListWrapper.Journeys.First().Should().BeEquivalentTo([firstFlightResult, secondFlightResult, thirdFlightResult]);
    }
}
