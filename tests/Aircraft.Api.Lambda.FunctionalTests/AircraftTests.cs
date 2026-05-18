using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Shared.Contracts;

namespace Aircraft.Api.Lambda.FunctionalTests;

public class AircraftTests : BaseFunctionalTest
{
    private readonly CreateAircraftDto _request = new CreateAircraftDto("C-FJRN", "B78X", 135500, "Parked", 254011, "CYVR", null, 201848, 192777, 101522);
    public AircraftTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_Should_ReturnNotFound_WhenSeatLayoutDefinitionDoesNotExist()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FZTY", "B38M", 42045, "Parked", 82190, "CYVR", null, 69308, 65952, 20826);
        var error = "Seat layout definition for B38M not found";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRO", "B78X", 135500, "Parked", 0, "CYVR", null, 201848, 192777, 101522);
        var error = "Maximum takeoff weight must be greater than zero.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsInvalid()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRP", "B78X", 135500, "Delayed", 254011, null, null, 201848, 192777, 101522);
        var error = "Delayed is not a valid status";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsParkedAndParkedAtIsNotProvided()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRQ", "B78X", 135500, "Parked", 254011, null, null, 201848, 192777, 101522);
        var error = "Error creating aircraft: Status is Parked, so ParkedAt must be provided.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsParkedAndEnRouteToIsProvided()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRR", "B78X", 135500, "Parked", 254011, "CYVR", "CYYZ", 201848, 192777, 101522);
        var error = "Error creating aircraft: Status is Parked, so EnRouteTo must be empty.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsEnRouteAndEnRouteToIsNotProvided()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRS", "B78X", 135500, "EnRoute", 254011, null, null, 201848, 192777, 101522);
        var error = "Error creating aircraft: Status is EnRoute, so EnRouteTo must be provided.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsEnRouteAndParkedAtIsProvided()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRT", "B78X", 135500, "EnRoute", 254011, "CYVR", "CYYZ", 201848, 192777, 101522);
        var error = "Error creating aircraft: Status is EnRoute, so ParkedAt must be empty.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task GetById_Should_ReturnNotFound_WhenAircraftDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = $"Aircraft with ID {id} not found";

        // Act
        var uri = new Uri($"aircraft/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Create_Should_ReturnCreated_WhenRequestIsValid()
    {
        var aircraft = await CreateAircraftAsync();
        aircraft.Should().Match<AircraftDto>(x =>
            x.TailNumber == _request.TailNumber &&
            x.EquipmentCode == _request.EquipmentCode &&
            x.DryOperatingWeight == _request.DryOperatingWeight &&
            x.MaximumFuelWeight == _request.MaximumFuelWeight &&
            x.MaximumLandingWeight == _request.MaximumLandingWeight &&
            x.MaximumTakeoffWeight == _request.MaximumTakeoffWeight &&
            x.MaximumZeroFuelWeight == _request.MaximumZeroFuelWeight &&
            x.Seats == 337);
    }

    private async Task<AircraftDto> CreateAircraftAsync()
    {
        var response = await HttpClient.PostAsJsonAsync("aircraft", _request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var aircraft = await JsonSerializer.DeserializeAsync<AircraftDto>(content, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        if (aircraft is null)
        {
            throw new JsonException("Deserialized aircraft is null");
        }
        return aircraft;
    }
}
