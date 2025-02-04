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
    public async Task Should_ReturnBadRequest_WhenDepartureIsEmpty()
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
    public async Task Should_ReturnBadRequest_WhenDestinationIsEmpty()
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
}
