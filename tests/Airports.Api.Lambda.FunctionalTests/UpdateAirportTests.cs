using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Shared.Contracts;

namespace Airports.Api.Lambda.FunctionalTests;

public class UpdateAirportTests : BaseFunctionalTest
{
    public UpdateAirportTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIcaoCodeIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("", "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIcaoCodeIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto(LongString, "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await HttpClient.PutAsJsonAsync($"airports/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenAirportDoesNotExist()
    {
        // Arrange
        await EnsureDynamoDbTableCreatedAsync();
        var nonExistentId = Guid.NewGuid();
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "America/Vancouver");
        var expectedError = $"Airport with id {nonExistentId} not found";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{nonExistentId}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await GetProblemDetailsFromResponseAndAssert(response, expectedError);
    }

    [Fact]
    public async Task Should_UpdateAirport_WhenAirportExists()
    {
        // Arrange
        var createRequest = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "America/Vancouver");
        var createResponse = await HttpClient.PostAsJsonAsync("airports", createRequest, TestContext.Current.CancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var createResponseContent = await createResponse.Content.ReadFromJsonAsync<Airport>(TestContext.Current.CancellationToken) ?? throw new JsonException("Expected airport object not returned");
        var updateRequest = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver Intercontinental Airport", "America/Vancouver");
        var expected = new AirportDto(createResponseContent.Id, "Vancouver Intercontinental Airport", "CYVR", "YVR", "America/Vancouver");

        // Act
        var updateResponse = await HttpClient.PutAsJsonAsync($"airports/{createResponseContent.Id}", updateRequest, TestContext.Current.CancellationToken);
        updateResponse.EnsureSuccessStatusCode();
        var updateResponseContent = await updateResponse.Content.ReadFromJsonAsync<Airport>(TestContext.Current.CancellationToken) ?? throw new JsonException("Expected airport object not returned");

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updateResponseContent.Should().BeEquivalentTo(expected);
    }
}
