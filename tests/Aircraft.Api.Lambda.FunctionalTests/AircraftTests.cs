using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Shared.Contracts;

namespace Aircraft.Api.Lambda.FunctionalTests;

public class AircraftTests : BaseFunctionalTest
{
    public AircraftTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_Should_ReturnNotFound_WhenSeatLayoutDefinitionDoesNotExist()
    {
        // Arrange
        var situation = new AircraftSituationDto("Parked", "CYVR");
        var request = new CreateAircraftDto("C-FZTY", "B38M", 42045, 82190, 69308, 65952, 20826, situation, TimeSpan.FromHours(0));
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
        var situation = new AircraftSituationDto("Parked", "CYVR");
        var request = new CreateAircraftDto("C-FJRO", "B78X", 135500, 0, 201848, 192777, 101522, situation, TimeSpan.FromHours(0));
        var error = "Maximum takeoff weight must be greater than zero.";

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
    public async Task List_Should_ReturnBadRequest_WhenFilteringByParkedAtAndEnRouteTo()
    {
        // Act
        var uri = new Uri("aircraft?parkedAt=CYVR&enRouteTo=CYYZ", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(
            response,
            "Cannot filter by both parkedAt and enRouteTo simultaneously.");
    }

    [Fact]
    public async Task Aircraft_Lifecycle_Should_Succeed()
    {
        var ct = TestContext.Current.CancellationToken;

        // List should return empty list when no aircraft match
        var emptyListUri = new Uri("aircraft", UriKind.Relative);
        var emptyListResponse = await HttpClient.GetAsync(emptyListUri, ct);
        emptyListResponse.EnsureSuccessStatusCode();
        var emptyListContent = await emptyListResponse.Content.ReadAsStreamAsync(ct);
        var emptyAircraftList = await JsonSerializer.DeserializeAsync<AircraftListDto>(emptyListContent, JsonSerializerOptions.Web, ct);
        emptyAircraftList.Should().BeEquivalentTo(new AircraftListDto([], 1, 50, 0, false));

        // Create
        var request = new CreateAircraftDto("C-FJRN", "B78X", 135500, 254011, 201848, 192777, 101522, new AircraftSituationDto("Parked", "CYVR"), TimeSpan.FromHours(0));
        var aircraft = await CreateAircraftAsync(request);
        aircraft.Should().Match<AircraftDto>(x =>
            x.TailNumber == request.TailNumber &&
            x.EquipmentCode == request.EquipmentCode &&
            x.DryOperatingWeight == request.DryOperatingWeight &&
            x.MaximumFuelWeight == request.MaximumFuelWeight &&
            x.MaximumLandingWeight == request.MaximumLandingWeight &&
            x.MaximumTakeoffWeight == request.MaximumTakeoffWeight &&
            x.MaximumZeroFuelWeight == request.MaximumZeroFuelWeight &&
            x.Situation == request.Situation &&
            x.Seats == 337);

        // Create should return conflict when creating aircraft with same tail number
        var conflictResponse = await HttpClient.PostAsJsonAsync("aircraft", request, ct);
        await GetProblemDetailsFromResponseAndAssert(conflictResponse, $"Aircraft with tail number {request.TailNumber} already exists");

        // List
        var expected = new AircraftListDto([aircraft], 1, 50, 1, false);
        var listUri = new Uri("aircraft", UriKind.Relative);
        var listResponse = await HttpClient.GetAsync(listUri, ct);
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadAsStreamAsync(ct);
        var aircraftList = await JsonSerializer.DeserializeAsync<AircraftListDto>(listContent, JsonSerializerOptions.Web, ct);
        aircraftList.Should().BeEquivalentTo(expected);

        // List filtered by parkedAt
        var listFilterUri = new Uri($"aircraft?parkedAt={request.Situation.AirportIcaoCode}", UriKind.Relative);
        var listFilterResponse = await HttpClient.GetAsync(listFilterUri, ct);
        listFilterResponse.EnsureSuccessStatusCode();
        var listFilterContent = await listFilterResponse.Content.ReadAsStreamAsync(ct);
        var aircraftListFilter = await JsonSerializer.DeserializeAsync<AircraftListDto>(listFilterContent, JsonSerializerOptions.Web, ct);
        aircraftListFilter.Should().BeEquivalentTo(expected);

        // List should return last page when requested page exceeds available pages
        var listExceedPageUri = new Uri("aircraft?page=2", UriKind.Relative);
        var listExceedPageResponse = await HttpClient.GetAsync(listExceedPageUri, ct);
        listExceedPageResponse.EnsureSuccessStatusCode();
        var listExceedPageContent = await listExceedPageResponse.Content.ReadAsStreamAsync(ct);
        var aircraftListExceedPage = await JsonSerializer.DeserializeAsync<AircraftListDto>(listExceedPageContent, JsonSerializerOptions.Web, ct);
        aircraftListExceedPage.Should().BeEquivalentTo(expected);

        // List filtered by enRouteTo
        var req2 = request with { TailNumber = "C-FJRO", Situation = new AircraftSituationDto("EnRoute", "CYYZ") };
        var aircraft2 = await CreateAircraftAsync(req2);
        var expected2 = new AircraftListDto([aircraft2], 1, 50, 1, false);
        var listFilterUri2 = new Uri($"aircraft?enRouteTo={req2.Situation.AirportIcaoCode}", UriKind.Relative);
        var listFilterResponse2 = await HttpClient.GetAsync(listFilterUri2, ct);
        listFilterResponse2.EnsureSuccessStatusCode();
        var listFilterContent2 = await listFilterResponse2.Content.ReadAsStreamAsync(ct);
        var aircraftListFilter2 = await JsonSerializer.DeserializeAsync<AircraftListDto>(listFilterContent2, JsonSerializerOptions.Web, ct);
        aircraftListFilter2.Should().BeEquivalentTo(expected2);
    }

    private async Task<AircraftDto> CreateAircraftAsync(CreateAircraftDto request)
    {
        var response = await HttpClient.PostAsJsonAsync("aircraft", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var aircraft = await JsonSerializer.DeserializeAsync<AircraftDto>(content, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        if (aircraft is null)
        {
            throw new JsonException("Deserialized aircraft is null");
        }
        return aircraft;
    }
}
