using System.Net;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Airports;

public class DeleteAirportTests : BaseFunctionalTest
{
    public DeleteAirportTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnNoContent_WhenRequestIsValid()
    {
        // Arrange
        var airport = await SeedAirportAsync(new CreateAirportDto("VIE", "Vienna International Airport", "Europe/Vienna"));

        // Act
        var uri = new Uri($"/airports/{airport.Id}", UriKind.Relative);
        var response = await HttpClient.DeleteAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
