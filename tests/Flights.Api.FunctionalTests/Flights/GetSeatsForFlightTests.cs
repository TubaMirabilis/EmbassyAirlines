using System.Net;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;

namespace Flights.Api.FunctionalTests.Flights;

public class GetSeatsForFlightTests : BaseFunctionalTest
{
    public GetSeatsForFlightTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenFlightIdIsEmpty()
    {
        // Arrange
        var id = Guid.Empty;
        var error = "FlightId is required.";

        // Act
        var uri = new Uri($"flights/{id}/seats", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }
}
