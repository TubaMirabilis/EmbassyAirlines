using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Shared.Contracts;

namespace Aircraft.Api.Lambda.FunctionalTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class AircraftTests : BaseFunctionalTest
{
    private static AircraftDto? s_dto;
    public AircraftTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact, TestPriority(0)]
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

    [Fact, TestPriority(1)]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsParkedAndParkedAtIsNotProvided()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRN", "B78X", 135500, "Parked", 254011, null, null, 201848, 192777, 101522);
        var error = "Error creating aircraft: Status is Parked, so ParkedAt must be provided.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(2)]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsParkedAndEnRouteToIsProvided()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRN", "B78X", 135500, "Parked", 254011, "CYVR", "CYYZ", 201848, 192777, 101522);
        var error = "Error creating aircraft: Status is Parked, so EnRouteTo must be empty.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(3)]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsEnRouteAndEnRouteToIsNotProvided()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRN", "B78X", 135500, "EnRoute", 254011, null, null, 201848, 192777, 101522);
        var error = "Error creating aircraft: Status is EnRoute, so EnRouteTo must be provided.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(4)]
    public async Task Create_Should_ReturnBadRequest_WhenStatusIsEnRouteAndParkedAtIsProvided()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRN", "B78X", 135500, "EnRoute", 254011, "CYVR", "CYYZ", 201848, 192777, 101522);
        var error = "Error creating aircraft: Status is EnRoute, so ParkedAt must be empty.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(5)]
    public async Task Create_Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRN", "B78X", 135500, "Parked", 254011, "CYVR", null, 201848, 192777, 101522);

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var aircraft = await JsonSerializer.DeserializeAsync<AircraftDto>(content, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        if (aircraft is null)
        {
            throw new JsonException("Deserialized aircraft is null");
        }
        s_dto = aircraft;

        // Assert
        s_dto.Should().Match<AircraftDto>(x =>
            x.TailNumber == request.TailNumber &&
            x.EquipmentCode == request.EquipmentCode &&
            x.DryOperatingWeight == request.DryOperatingWeight &&
            x.MaximumFuelWeight == request.MaximumFuelWeight &&
            x.MaximumLandingWeight == request.MaximumLandingWeight &&
            x.MaximumTakeoffWeight == request.MaximumTakeoffWeight &&
            x.MaximumZeroFuelWeight == request.MaximumZeroFuelWeight &&
            x.Seats == 337);
    }

    [Fact, TestPriority(6)]
    public async Task Create_Should_ReturnConflict_WhenAircraftAlreadyExists()
    {
        // Arrange
        var request = new CreateAircraftDto("C-FJRN", "B78X", 135500, "Parked", 254011, "CYVR", null, 201848, 192777, 101522);
        var error = $"Aircraft with tail number {request.TailNumber} already exists";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(7)]
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

    [Fact, TestPriority(8)]
    public async Task GetById_Should_ReturnOk_WhenAircraftExists()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var id = s_dto.Id;

        // Act
        var uri = new Uri($"aircraft/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var aircraftDto = await response.Content.ReadFromJsonAsync<AircraftDto>(TestContext.Current.CancellationToken);

        // Assert
        aircraftDto.Should().BeEquivalentTo(s_dto);
    }

    [Fact, TestPriority(9)]
    public async Task List_Should_ReturnOk_WhenAircraftExist()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var expected = new AircraftListDto([s_dto], 1, 50, 1, false);

        // Act
        var uri = new Uri("aircraft", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var aircraftListDto = await response.Content.ReadFromJsonAsync<AircraftListDto>(TestContext.Current.CancellationToken);

        // Assert
        aircraftListDto.Should().BeEquivalentTo(expected);
    }

    [Fact, TestPriority(10)]
    public async Task List_Should_ReturnOk_WhenAircraftExistAndQueryStringParametersAreUsed()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var expected = new AircraftListDto([s_dto], 1, 50, 1, false);
        var parkedAt = "CYVR";

        // Act
        var uri = new Uri($"aircraft?parkedAt={parkedAt}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var aircraftListDto = await response.Content.ReadFromJsonAsync<AircraftListDto>(TestContext.Current.CancellationToken);

        // Assert
        aircraftListDto.Should().BeEquivalentTo(expected);
    }
}
