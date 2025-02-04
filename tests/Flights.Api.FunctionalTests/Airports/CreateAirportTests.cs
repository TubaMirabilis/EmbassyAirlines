using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Airports;

public class CreateAirportTests : BaseFunctionalTest
{
    private readonly string _longString;
    public CreateAirportTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        _longString = new string('A', 101);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsInvalid()
    {
        // Arrange
        var request = new CreateAirportDto("", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must be 3 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateAirportDto("YVR", "", "America/Vancouver");
        var error = "Name is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenNameIsTooLong()
    {
        // Arrange
        var request = new CreateAirportDto("YVR", _longString, "America/Vancouver");
        var error = "Name must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenTimeZoneIdIsEmpty()
    {
        // Arrange
        var request = new CreateAirportDto("YVR", "Vancouver International Airport", "");
        var error = "Time zone is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenTimeZoneIdIsTooLong()
    {
        // Arrange
        var request = new CreateAirportDto("YVR", "Vancouver International Airport", _longString);
        var error = "Time zone must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateAirportDto("LHR", "London Heathrow Airport", "Europe/London");

        // Act
        var response = await SeedAirportAsync(request);

        // Assert
        response.Name.Should().Be(request.Name);
        response.IataCode.Should().Be(request.IataCode);
        response.TimeZoneId.Should().Be(request.TimeZoneId);
    }

    [Fact]
    public async Task Should_ReturnConflict_WhenAirportExists()
    {
        // Arrange
        var request = new CreateAirportDto("YVR", "Vancouver International Airport", "America/Vancouver");
        await SeedAirportAsync(request);

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
