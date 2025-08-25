using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Shared.Contracts;

namespace Aircraft.Api.Lambda.FunctionalTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class AircraftTests : BaseFunctionalTest
{
    private static AircraftDto? _dto;
    public AircraftTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact, TestPriority(1)]
    public async Task Test1()
    {
        // Arrange
        var request = new CreateOrUpdateAircraftDto("C-FZTY", "B38M", 42045, 82190, 69308, 65952, 20826);
        var error = "Seat layout definition for B38M not found";

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(1)]
    public async Task Create_Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateOrUpdateAircraftDto("C-FJRN", "B78X", 135500, 254011, 201848, 192777, 101522);

        // Act
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        _dto = await JsonSerializer.DeserializeAsync<AircraftDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken) ?? throw new JsonException();

        // Assert
        _dto.Should().Match<AircraftDto>(x =>
            x.TailNumber == request.TailNumber &&
            x.EquipmentCode == request.EquipmentCode &&
            x.DryOperatingWeight == request.DryOperatingWeight &&
            x.MaximumFuelWeight == request.MaximumFuelWeight &&
            x.MaximumLandingWeight == request.MaximumLandingWeight &&
            x.MaximumTakeoffWeight == request.MaximumTakeoffWeight &&
            x.MaximumZeroFuelWeight == request.MaximumZeroFuelWeight &&
            x.Seats.Count() == 337);
    }

    [Fact, TestPriority(2)]
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

    [Fact, TestPriority(3)]
    public async Task GetById_Should_ReturnOk_WhenAircraftExists()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(_dto);
        var id = _dto.Id;

        // Act
        var uri = new Uri($"aircraft/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var aircraftDto = await response.Content.ReadFromJsonAsync<AircraftDto>(TestContext.Current.CancellationToken);

        // Assert
        aircraftDto.Should().BeEquivalentTo(_dto);
    }
}
