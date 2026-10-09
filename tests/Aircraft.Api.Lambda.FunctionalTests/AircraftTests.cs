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
        // List should return empty list when no aircraft match
        var emptyListUri = new Uri("aircraft", UriKind.Relative);
        var emptyListResponse = await HttpClient.GetAsync(emptyListUri, TestContext.Current.CancellationToken);
        emptyListResponse.EnsureSuccessStatusCode();
        var emptyListContent = await emptyListResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var emptyAircraftList = await JsonSerializer.DeserializeAsync<AircraftListDto>(emptyListContent, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        emptyAircraftList.Should().BeEquivalentTo(new AircraftListDto([], 1, 50, 0, false));

        // Create
        var request = new CreateAircraftDto("C-FJRN", "B78X", 135500, "Parked", 254011, "CYVR", null, 201848, 192777, 101522);
        var aircraft = await CreateAircraftAsync(request);
        aircraft.Should().Match<AircraftDto>(x =>
            x.TailNumber == request.TailNumber &&
            x.EquipmentCode == request.EquipmentCode &&
            x.DryOperatingWeight == request.DryOperatingWeight &&
            x.MaximumFuelWeight == request.MaximumFuelWeight &&
            x.MaximumLandingWeight == request.MaximumLandingWeight &&
            x.MaximumTakeoffWeight == request.MaximumTakeoffWeight &&
            x.MaximumZeroFuelWeight == request.MaximumZeroFuelWeight &&
            x.Seats == 337);

        // List
        var expected = new AircraftListDto([aircraft], 1, 50, 1, false);
        var listUri = new Uri("aircraft", UriKind.Relative);
        var listResponse = await HttpClient.GetAsync(listUri, TestContext.Current.CancellationToken);
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var aircraftList = await JsonSerializer.DeserializeAsync<AircraftListDto>(listContent, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        aircraftList.Should().BeEquivalentTo(expected);

        // List filtered by parkedAt
        var listFilterUri = new Uri($"aircraft?parkedAt={request.ParkedAt}", UriKind.Relative);
        var listFilterResponse = await HttpClient.GetAsync(listFilterUri, TestContext.Current.CancellationToken);
        listFilterResponse.EnsureSuccessStatusCode();
        var listFilterContent = await listFilterResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var aircraftListFilter = await JsonSerializer.DeserializeAsync<AircraftListDto>(listFilterContent, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        aircraftListFilter.Should().BeEquivalentTo(expected);

        // List should return last page when requested page exceeds available pages
        var listExceedPageUri = new Uri("aircraft?page=2", UriKind.Relative);
        var listExceedPageResponse = await HttpClient.GetAsync(listExceedPageUri, TestContext.Current.CancellationToken);
        listExceedPageResponse.EnsureSuccessStatusCode();
        var listExceedPageContent = await listExceedPageResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var aircraftListExceedPage = await JsonSerializer.DeserializeAsync<AircraftListDto>(listExceedPageContent, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        aircraftListExceedPage.Should().BeEquivalentTo(expected);

        // List filtered by enRouteTo
        var req2 = request with { Status = "EnRoute", ParkedAt = null, EnRouteTo = "CYYZ", TailNumber = "C-FJRO" };
        var aircraft2 = await CreateAircraftAsync(req2);
        var expected2 = new AircraftListDto([aircraft2], 1, 50, 1, false);
        var listFilterUri2 = new Uri($"aircraft?enRouteTo={req2.EnRouteTo}", UriKind.Relative);
        var listFilterResponse2 = await HttpClient.GetAsync(listFilterUri2, TestContext.Current.CancellationToken);
        listFilterResponse2.EnsureSuccessStatusCode();
        var listFilterContent2 = await listFilterResponse2.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var aircraftListFilter2 = await JsonSerializer.DeserializeAsync<AircraftListDto>(listFilterContent2, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
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
