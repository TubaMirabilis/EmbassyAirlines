using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Flights;

public class ScheduleFlightTests : BaseFunctionalTest
{
    public ScheduleFlightTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenFlightNumberIsTooLong()
    {
        // Arrange
        var request = new ScheduleFlightDto(LongString, Guid.NewGuid(), DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = "Flight number must be 6 characters or less.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenFlightNumberIsEmpty()
    {
        // Arrange
        var request = new ScheduleFlightDto("", Guid.NewGuid(), DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = "Flight number must be alphanumeric.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenFlightNumberIsNotAlphanumeric()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX25!", Guid.NewGuid(), DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = "Flight number must be alphanumeric.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDepartureAirportIdIsEmpty()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX252", Guid.Empty, DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = "Departure airport id is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenArrivalAirportIdIsEmpty()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.Now.AddDays(1),
            Guid.Empty, DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = "Arrival airport id is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDepartureTimeIsEmpty()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.MinValue,
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = "Departure time is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenArrivalTimeIsEmpty()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.Now.AddDays(1), Guid.NewGuid(), DateTime.MinValue, 1000, 2000, "B78X");
        var error = "Arrival time is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenEconomyPriceIsNegative()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), decimal.MinValue, 2000, "B78X");
        var error = "Economy price must be greater than 0.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenBusinessPriceIsNegative()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, decimal.MinValue, "B78X");
        var error = "Business price must be greater than 0.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenEquipmentTypeIsEmpty()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "");
        var error = "Equipment type is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenEquipmentTypeIsTooLong()
    {
        // Arrange
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, LongString);
        var error = "Equipment type must be 4 characters or less.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenDepartureAirportDoesNotExist()
    {
        // Arrange
        var airportId = Guid.NewGuid();
        var request = new ScheduleFlightDto("EX252", airportId, DateTime.Now.AddDays(1),
            Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = $"Departure airport with id {airportId} not found.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenArrivalAirportDoesNotExist()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("JFK", "John F. Kennedy International Airport", "America/New_York"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirportId = Guid.NewGuid();
        var request = new ScheduleFlightDto("EX252", departureAirportId, DateTime.Now.AddDays(1),
            arrivalAirportId, DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = $"Arrival airport with id {arrivalAirportId} not found.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenDepartureTimeIsInThePast()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("YYZ", "Toronto Pearson International Airport", "America/Toronto"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("LAX", "Los Angeles International Airport", "America/Los_Angeles"));
        var arrivalAirportId = arrivalAirport.Id;
        var departureTime = DateTime.Now.Subtract(TimeSpan.FromDays(7));
        var request = new ScheduleFlightDto("EX252", departureAirportId, departureTime, arrivalAirportId,
            DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = "Departure time is in the past.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenArrivalTimeIsBeforeDepartureTime()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("YYC", "Calgary International Airport", "America/Edmonton"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("DEN", "Denver International Airport", "America/Denver"));
        var arrivalAirportId = arrivalAirport.Id;
        var request = new ScheduleFlightDto("EX252", departureAirportId, DateTime.Now.AddDays(1), arrivalAirportId, DateTime.Now, 1000, 2000, "B78X");
        var error = "Arrival time is before departure time.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var departureAirport = await SeedAirportAsync(new CreateAirportDto("CDG", "Charles de Gaulle Airport", "Europe/Paris"));
        var departureAirportId = departureAirport.Id;
        var arrivalAirport = await SeedAirportAsync(new CreateAirportDto("IAD", "Washington Dulles International Airport", "America/New_York"));
        var arrivalAirportId = arrivalAirport.Id;
        var now = DateTime.Now;
        var request = new ScheduleFlightDto("EX252", departureAirportId, now.AddDays(1),
            arrivalAirportId, now.AddDays(1).AddHours(2).AddMinutes(51), 1000, 2000, "B78X");

        // Act
        var response = await SeedFlightAsync(request);

        // Assert
        response.FlightNumber.Should().Be(request.FlightNumber);
        response.Departure.Should().Be("CDG");
        response.Destination.Should().Be("IAD");
        response.CheapestEconomyPrice.Should().Be(request.EconomyPrice);
        response.CheapestBusinessPrice.Should().Be(request.BusinessPrice);
        response.Duration.Should().Be(TimeSpan.FromMinutes(531));
        response.AvailableEconomySeats.Should().Be(301);
        response.AvailableBusinessSeats.Should().Be(36);
    }
}
