using System.Net;
using System.Net.Http.Json;
using Flights.Api.FunctionalTests.Abstractions;
using FluentAssertions;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Airports;

public class CreateAirportTests : BaseFunctionalTest
{
    public CreateAirportTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsEmpty()
    {
        // Arrange
        var request = new CreateAirportDto("", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsTooLong()
    {
        // Arrange
        var request = new CreateAirportDto(LongString, "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsInvalid()
    {
        // Arrange
        var request = new CreateAirportDto("3/a", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

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
        var request = new CreateAirportDto("YVR", LongString, "America/Vancouver");
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
        var request = new CreateAirportDto("YVR", "Vancouver International Airport", LongString);
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
