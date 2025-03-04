using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

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

    [Fact]
    public async Task Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = $"Flight with id {id} was not found.";

        // Act
        var uri = new Uri($"flights/{id}/seats", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenSeatTypeFilterIsApplied()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("HND", "Haneda Airport", "Asia/Tokyo"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("KIX", "Kansai International Airport", "Asia/Tokyo"));
        var arrivalAirportId = arrivalAirport.Id;
        var now = DateTime.Now;
        var flightRequest = new ScheduleFlightDto("EX462", departureAirportId, now.AddDays(1),
            arrivalAirportId, now.AddDays(1).AddHours(1).AddMinutes(1), 1000, 2000, "B78X");
        var flightResult = await SeedFlightAsync(flightRequest);
        var id = flightResult.Id;

        // Act
        var uri = new Uri($"flights/{id}/seats?seatType=Business", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var seats = await response.Content.ReadFromJsonAsync<List<SeatDto>>() ?? throw new InvalidOperationException("No seats found");
        seats.Should().HaveCount(36);
        seats.Should().OnlyContain(s => s.SeatType == "Business");
        seats.Should().OnlyContain(s => s.Price == 2000);
        seats.Should().OnlyContain(s => !s.IsBooked);
    }
}
