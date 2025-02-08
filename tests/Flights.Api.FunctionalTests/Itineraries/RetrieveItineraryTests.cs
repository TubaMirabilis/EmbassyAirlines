using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Itineraries;

public class RetrieveItineraryTests : BaseFunctionalTest
{
    public RetrieveItineraryTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenItineraryDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = "Itinerary not found.";

        // Act
        var uri = new Uri($"/itineraries/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("AGP", "Málaga Airport", "Europe/Madrid"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("LGW", "London Gatwick Airport", "Europe/London"));
        var arrivalAirportId = arrivalAirport.Id;
        var now = DateTime.Now;
        var flightRequest = new ScheduleFlightDto("EX62", departureAirportId, now.AddDays(1),
            arrivalAirportId, now.AddDays(1).AddHours(1).AddMinutes(46), 1000, 2000, "B78X");
        var flightResult = await SeedFlightAsync(flightRequest);
        var flightId = flightResult.Id;
        var seats = await HttpClient.GetFromJsonAsync<IEnumerable<SeatDto>>($"flights/{flightId}/seats");
        var seatId = seats?.FirstOrDefault()?.Id ?? throw new InvalidOperationException("No seats found");
        var newPassenger = new PassengerDto("Mark", "Zuckerberg");
        var passengers = new Dictionary<Guid, PassengerDto>()
        {
            { seatId, newPassenger }
        };
        var booking = new CreateBookingDto(passengers, flightId);
        var itinerary = new CreateItineraryDto([booking], null);
        var itineraryResult = await SeedItineraryAsync(itinerary);
        var id = itineraryResult.Reference;

        // Act
        var uri = new Uri($"/itineraries/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ItineraryDto>() ?? throw new InvalidOperationException("No content found");
        result.Bookings.Should().HaveCount(1);
        result.Bookings.First().Passengers.First().Value.Key.Should().BeEquivalentTo(newPassenger);
    }
}
