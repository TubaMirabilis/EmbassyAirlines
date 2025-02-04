using System.Net;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;

namespace Flights.Api.FunctionalTests.Airports;

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
}
