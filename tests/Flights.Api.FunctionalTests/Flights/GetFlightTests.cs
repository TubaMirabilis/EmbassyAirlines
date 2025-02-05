using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Flights;

public class GetFlightTests : BaseFunctionalTest
{
    public GetFlightTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = $"Flight with id {id} was not found.";

        // Act
        var uri = new Uri($"flights/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenFlightExists()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("CMN", "Mohammed V International Airport", "Africa/Casablanca"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("DOH", "Hamad International Airport", "Asia/Qatar"));
        var arrivalAirportId = arrivalAirport.Id;
        var now = DateTime.Now;
        var flightRequest = new ScheduleFlightDto("EX1862", departureAirportId, now.AddDays(1), arrivalAirportId, now.AddDays(1).AddHours(9), 1000, 2000, "B78X");
        var flightResult = await SeedFlightAsync(flightRequest);
        var id = flightResult.Id;

        // Act
        var uri = new Uri($"flights/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flightDto = await response.Content.ReadFromJsonAsync<FlightDto>();
        flightDto.Should().BeEquivalentTo(flightResult);
    }
}
