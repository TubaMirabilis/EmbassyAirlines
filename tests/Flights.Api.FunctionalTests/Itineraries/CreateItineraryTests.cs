using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Itineraries;

public class CreateItineraryTests : BaseFunctionalTest
{
    public CreateItineraryTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenBookingsIsEmpty()
    {
        // Arrange
        var request = new CreateItineraryDto([], null);
        var error = "Please provide at least one booking request.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("itineraries", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenBookingHasNoSeats()
    {
        // Arrange
        var dic = new Dictionary<Guid, PassengerDto>();
        var booking = new CreateBookingDto(dic, Guid.NewGuid());
        var request = new CreateItineraryDto([booking], null);
        var error = "Please provide at least one seat for each booking.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("itineraries", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenBookingFlightIdIsEmpty()
    {
        // Arrange
        var dic = new Dictionary<Guid, PassengerDto>
        {
            { Guid.NewGuid(), new PassengerDto("Mark", "Zuckerberg") }
        };
        var booking = new CreateBookingDto(dic, Guid.Empty);
        var request = new CreateItineraryDto([booking], null);
        var error = "FlightId is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("itineraries", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        // Arrange
        var dic = new Dictionary<Guid, PassengerDto>
        {
            { Guid.NewGuid(), new PassengerDto("Mark", "Zuckerberg") }
        };
        var booking = new CreateBookingDto(dic, Guid.NewGuid());
        var request = new CreateItineraryDto([booking], null);
        var error = "Flight not found.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("itineraries", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenSeatIdIsInvalid()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("FRA", "Frankfurt Airport", "Europe/Berlin"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("ORD", "O'Hare International Airport", "America/Chicago"));
        var arrivalAirportId = arrivalAirport.Id;
        var now = DateTime.Now;
        var flightRequest = new ScheduleFlightDto("EX862", departureAirportId, now.AddDays(1),
            arrivalAirportId, now.AddDays(1).AddHours(1).AddMinutes(35), 1000, 2000, "B78X");
        var flightResult = await SeedFlightAsync(flightRequest);
        var flightId = flightResult.Id;
        var passenger = new PassengerDto("Mark", "Zuckerberg");
        var passengers = new Dictionary<Guid, PassengerDto>()
        {
            { Guid.NewGuid(), passenger }
        };
        var booking = new CreateBookingDto(passengers, flightId);
        var itinerary = new CreateItineraryDto([booking], null);

        // Act
        var response = await HttpClient.PostAsJsonAsync("itineraries", itinerary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        var flightRequest = new ScheduleFlightDto("EX262", departureAirportId, now.AddDays(1),
            arrivalAirportId, now.AddDays(1).AddHours(5).AddMinutes(47), 1000, 2000, "B78X");
        var flightResult = await SeedFlightAsync(flightRequest);
        var flightId = flightResult.Id;
        var seats = await HttpClient.GetFromJsonAsync<IEnumerable<SeatDto>>($"flights/{flightId}/seats");
        var seat = seats?.FirstOrDefault() ?? throw new InvalidOperationException("No seats found");
        var seatId = seat.Id;
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
        response.Bookings.First().FlightNumber.Should().Be("EX262");
        response.Bookings.First().Passengers.Should().HaveCount(1);
        response.Reference.Length.Should().BeGreaterThanOrEqualTo(6);
        response.LeadPassengerEmail.Should().BeEmpty();
        response.TotalPrice.Should().Be(2000);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenSeatIsAlreadyBooked()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("HGH", "Hangzhou Xiaoshan International Airport", "Asia/Shanghai"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("TPE", "Taiwan Taoyuan International Airport", "Asia/Taipei"));
        var arrivalAirportId = arrivalAirport.Id;
        var now = DateTime.Now;
        var flightRequest = new ScheduleFlightDto("EX212", departureAirportId, now.AddDays(1),
            arrivalAirportId, now.AddDays(1).AddHours(2).AddMinutes(1), 1000, 2000, "B78X");
        var flightResult = await SeedFlightAsync(flightRequest);
        var flightId = flightResult.Id;
        var seats = await HttpClient.GetFromJsonAsync<IEnumerable<SeatDto>>($"flights/{flightId}/seats");
        var seatId = seats?.Skip(1).Take(1).FirstOrDefault()?.Id ?? throw new InvalidOperationException("No seats found");
        var passenger = new PassengerDto("Mark", "Zuckerberg");
        var passengers = new Dictionary<Guid, PassengerDto>()
        {
            { seatId, passenger }
        };
        var booking = new CreateBookingDto(passengers, flightId);
        var itinerary = new CreateItineraryDto([booking], null);
        await SeedItineraryAsync(itinerary);
        var error = "One or more seats are already booked.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("itineraries", itinerary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }
}
