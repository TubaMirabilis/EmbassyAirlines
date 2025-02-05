using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Airports;

public class GetAirportTests : BaseFunctionalTest
{
    public GetAirportTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenAirportDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = $"Airport with id {id} was not found.";

        // Act
        var uri = new Uri($"airports/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenAirportExists()
    {
        // Arrange
        var airport = await SeedAirportAsync(new CreateAirportDto("EWR", "Newark Liberty International Airport", "America/New_York"));
        var id = airport.Id;

        // Act
        var uri = new Uri($"airports/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var airportDto = await response.Content.ReadFromJsonAsync<AirportDto>();
        airportDto.Should().BeEquivalentTo(airport);
    }
}
