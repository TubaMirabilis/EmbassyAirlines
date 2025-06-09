using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Airports.Api.Lambda.FunctionalTests;

public class CreateAirportTests : BaseFunctionalTest
{
    public CreateAirportTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIcaoCodeIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("", "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIcodeCodeIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto(LongString, "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIcaoCodeIsInvalid()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("3/a.", "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", LongString, "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsInvalid()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "3/a", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "", "America/Vancouver");
        var error = "Name is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenNameIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", LongString, "America/Vancouver");
        var error = "Name must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenTimeZoneIdIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "");
        var error = "Time zone is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenTimeZoneIdIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", LongString);
        var error = "Time zone must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "LHR", "London Heathrow Airport", "Europe/London");

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
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "America/Vancouver");
        await SeedAirportAsync(request);

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
