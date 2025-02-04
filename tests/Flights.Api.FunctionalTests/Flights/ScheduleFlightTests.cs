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
        var request = new ScheduleFlightDto(LongString, Guid.NewGuid(), DateTime.Now.AddDays(1), Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
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
        var request = new ScheduleFlightDto("", Guid.NewGuid(), DateTime.Now.AddDays(1), Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
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
        var request = new ScheduleFlightDto("EX25!", Guid.NewGuid(), DateTime.Now.AddDays(1), Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
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
        var request = new ScheduleFlightDto("EX252", Guid.Empty, DateTime.Now.AddDays(1), Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
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
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.Now.AddDays(1), Guid.Empty, DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), 1000, 2000, "B78X");
        var error = "Arrival airport id is required.";

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
        var request = new ScheduleFlightDto("EX252", Guid.NewGuid(), DateTime.Now.AddDays(1), Guid.NewGuid(), DateTime.Now.AddDays(1).AddHours(17).AddMinutes(25), decimal.MinValue, 2000, "B78X");
        var error = "Economy price must be greater than 0.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }
}
