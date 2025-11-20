using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Shared.Contracts;

namespace Airports.Api.Lambda.FunctionalTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class AirportsTests : BaseFunctionalTest
{
    private static AirportDto? s_dto;
    public AirportsTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact, TestPriority(0)]
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

    [Fact, TestPriority(1)]
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

    [Fact, TestPriority(2)]
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

    [Fact, TestPriority(3)]
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

    [Fact, TestPriority(4)]
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

    [Fact, TestPriority(5)]
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

    [Fact, TestPriority(6)]
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

    [Fact, TestPriority(7)]
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

    [Fact, TestPriority(8)]
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

    [Fact, TestPriority(9)]
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

    [Fact, TestPriority(10)]
    public async Task Create_Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "America/Vancouver");

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var airport = await JsonSerializer.DeserializeAsync<AirportDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken);
        if (airport is null)
        {
            throw new JsonException("Expected airport object not returned");
        }
        s_dto = airport;

        // Assert
        s_dto.Should().Match<AirportDto>(x =>
            x.Name == request.Name &&
            x.IataCode == request.IataCode &&
            x.TimeZoneId == request.TimeZoneId);
    }

    [Fact, TestPriority(11)]
    public async Task Create_Should_ReturnConflict_WhenAirportExists()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = $"Airport with IATA code {request.IataCode} already exists";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(12)]
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

    [Fact, TestPriority(13)]
    public async Task GetById_Should_ReturnOk_WhenAirportExists()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var id = s_dto.Id;

        // Act
        var uri = new Uri($"airports/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var airportDto = await response.Content.ReadFromJsonAsync<AirportDto>(TestContext.Current.CancellationToken);

        // Assert
        airportDto.Should().BeEquivalentTo(s_dto);
    }

    [Fact, TestPriority(14)]
    public async Task List_Should_ReturnOk_WhenAirportsExist()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var expected = new List<AirportDto> { s_dto };

        // Act
        var uri = new Uri("airports", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var airportDtos = await response.Content.ReadFromJsonAsync<List<AirportDto>>(TestContext.Current.CancellationToken);

        // Assert
        airportDtos.Should().BeEquivalentTo(expected);
    }

    [Fact, TestPriority(15)]
    public async Task Update_Should_ReturnBadRequest_WhenIcaoCodeIsEmpty()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("", "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(16)]
    public async Task Update_Should_ReturnBadRequest_WhenIcaoCodeIsTooLong()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto(LongString, "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(17)]
    public async Task Update_Should_ReturnBadRequest_WhenIcaoCodeIsInvalid()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("3/a.", "YVR", "Vancouver International Airport", "America/Vancouver");
        var error = "ICAO Code must consist of 4 uppercase letters only.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(18)]
    public async Task Update_Should_ReturnBadRequest_WhenIataCodeIsEmpty()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("CYVR", "", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(19)]
    public async Task Update_Should_ReturnBadRequest_WhenIataCodeIsTooLong()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("CYVR", LongString, "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(20)]
    public async Task Update_Should_ReturnBadRequest_WhenIataCodeIsInvalid()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("CYVR", "3/a", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(21)]
    public async Task Update_Should_ReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "", "America/Vancouver");
        var error = "Name is required.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(22)]
    public async Task Update_Should_ReturnBadRequest_WhenNameIsTooLong()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", LongString, "America/Vancouver");
        var error = "Name must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(23)]
    public async Task Update_Should_ReturnBadRequest_WhenTimeZoneIdIsEmpty()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "");
        var error = "Time zone is required.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(24)]
    public async Task Update_Should_ReturnBadRequest_WhenTimeZoneIdIsTooLong()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", LongString);
        var error = "Time zone must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(25)]
    public async Task Update_Should_ReturnNotFound_WhenAirportDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver International Airport", "America/Vancouver");
        var expectedError = $"Airport with id {id} not found";

        // Act
        var response = await HttpClient.PutAsJsonAsync($"airports/{id}", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, expectedError);
    }

    [Fact, TestPriority(26)]
    public async Task Update_Should_UpdateAirport_WhenAirportExists()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var updateRequest = new CreateOrUpdateAirportDto("CYVR", "YVR", "Vancouver Intercontinental Airport", "America/Vancouver");
        var expected = new AirportDto(s_dto.Id, "Vancouver Intercontinental Airport", "CYVR", "YVR", "America/Vancouver");

        // Act
        var updateResponse = await HttpClient.PutAsJsonAsync($"airports/{s_dto.Id}", updateRequest, TestContext.Current.CancellationToken);
        updateResponse.EnsureSuccessStatusCode();
        var updateResponseContent = await updateResponse.Content.ReadFromJsonAsync<AirportDto>(TestContext.Current.CancellationToken);
        if (updateResponseContent is null)
        {
            throw new JsonException("Expected airport object not returned");
        }

        // Assert
        updateResponseContent.Should().BeEquivalentTo(expected);
    }
}
