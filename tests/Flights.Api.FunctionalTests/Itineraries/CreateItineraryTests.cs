using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Airports;

public class CreateItineraryTests : BaseFunctionalTest
{
    public CreateItineraryTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("ICN", "Incheon International Airport", "Asia/Seoul"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("DPS", "Ngurah Rai International Airport", "Asia/Makassar"));
        var arrivalAirportId = arrivalAirport.Id;
        var now = DateTime.Now;
        var flightRequest = new ScheduleFlightDto("EX262", departureAirportId, now.AddDays(1), arrivalAirportId, now.AddDays(1).AddHours(5).AddMinutes(47), 1000, 2000, "B78X");
        var flightResult = await SeedFlightAsync(flightRequest);
        var flightId = flightResult.Id;
        var seats = await HttpClient.GetFromJsonAsync<IEnumerable<SeatDto>>($"flights/{flightId}/seats");
        var seatId = seats?.FirstOrDefault()?.Id ?? throw new InvalidOperationException("No seats found");
        var passenger = new PassengerDto("Mark", "Zuckerberg");
        var passengers = new Dictionary<Guid, PassengerDto>()
        {
            { seatId, passenger }
        };
        var booking = new CreateBookingDto(passengers, flightId);
        var itinerary = new CreateItineraryDto([booking], null);

        // Act
        var response = await SeedItineraryAsync(itinerary);

        // Assert
        response.Bookings.Should().HaveCount(1);
        response.Reference.Length.Should().BeGreaterThanOrEqualTo(6);
        response.LeadPassengerEmail.Should().BeEmpty();
        response.TotalPrice.Should().Be(2000);
    }
}
