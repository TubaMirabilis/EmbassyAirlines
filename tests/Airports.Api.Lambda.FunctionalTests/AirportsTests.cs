using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Shared.Contracts;

namespace Airports.Api.Lambda.FunctionalTests;

public class AirportsTests : BaseFunctionalTest
{
    public AirportsTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenIcaoCodeIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("", "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenIcodeCodeIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto(LongString, "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenIcaoCodeIsInvalid()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("3/a.", "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenIataCodeIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenIataCodeIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", LongString, "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenIataCodeIsInvalid()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "3/a", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "", "America/Vancouver");
        var error = "Name is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenNameIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", LongString, "America/Vancouver");
        var error = "Name must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenTimeZoneIdIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "");
        var error = "Time zone is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenTimeZoneIdIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", LongString);
        var error = "Time zone must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "America/Vancouver");

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var airport = await JsonSerializer.DeserializeAsync<AirportDto>(content, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        if (airport is null)
        {
            throw new JsonException("Expected airport object not returned");
        }

        // Assert
        airport.Should().Match<AirportDto>(x =>
            x.Name == request.Name &&
            x.IataCode == request.IataCode &&
            x.TimeZoneId == request.TimeZoneId);
    }

    [Fact]
    public async Task GetById_Should_ReturnNotFound_WhenAirportDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = $"Airport with id {id} not found";

        // Act
        var uri = new Uri($"airports/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }
}
