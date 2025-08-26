using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NodaTime;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class FlightsTests : BaseFunctionalTest
{
    private static FlightDto? s_dto;
    private readonly Airport _incheon;
    private readonly Airport _schipol;
    private readonly Aircraft _aircraft1;
    private readonly Aircraft _aircraft2;
    public FlightsTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        ArgumentNullException.ThrowIfNull(factory.IncheonAirport);
        ArgumentNullException.ThrowIfNull(factory.SchipolAirport);
        ArgumentNullException.ThrowIfNull(factory.Aircraft1);
        ArgumentNullException.ThrowIfNull(factory.Aircraft2);
        _incheon = factory.IncheonAirport;
        _schipol = factory.SchipolAirport;
        _aircraft1 = factory.Aircraft1;
        _aircraft2 = factory.Aircraft2;
    }

    [Fact, TestPriority(0)]
    public async Task Create_Should_ReturnBadRequest_WhenDepartureTimeIsInThePast()
    {
        // Arrange
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var recently = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromMinutes(30));
        var now = SystemClock.Instance.GetCurrentInstant();
        var soon = now.Plus(Duration.FromMinutes(30));
        var departureFromIncheon = recently.InZone(tz1).ToDateTimeUnspecified();
        var arrivalAtSchipol = soon.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30);
        var request = new CreateOrUpdateFlightDto(_aircraft1.Id, "EB1", "EBY1", _incheon.Id, departureFromIncheon, _schipol.Id, arrivalAtSchipol, 400, 4000);
        var error = "Departure time cannot be in the past";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(1)]
    public async Task Create_Should_ReturnBadRequest_WhenArrivalTimeIsBeforeDepartureTime()
    {
        // Arrange
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var now = SystemClock.Instance.GetCurrentInstant();
        var soon = now.Plus(Duration.FromMinutes(30));
        var departureFromIncheon = soon.InZone(tz1).ToDateTimeUnspecified();
        var arrivalAtSchipol = now.InZone(tz2).ToDateTimeUnspecified();
        var request = new CreateOrUpdateFlightDto(_aircraft1.Id, "EB1", "EBY1", _incheon.Id, departureFromIncheon, _schipol.Id, arrivalAtSchipol, 400, 4000);
        var error = "Arrival time cannot be before departure time";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(2)]
    public async Task Create_Should_ReturnNotFound_WhenAircraftDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var soon = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(30));
        var departureFromIncheon = soon.InZone(tz1).ToDateTimeUnspecified();
        var arrivalAtSchipol = soon.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30);
        var request = new CreateOrUpdateFlightDto(id, "EB1", "EBY1", _incheon.Id, departureFromIncheon, _schipol.Id, arrivalAtSchipol, 400, 4000);
        var error = $"Aircraft with ID {id} not found";

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(3)]
    public async Task Create_Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var soon = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(30));
        var departureFromIncheon = soon.InZone(tz1).ToDateTimeUnspecified();
        var arrivalAtSchipol = soon.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30);
        var duration = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30));
        var request = new CreateOrUpdateFlightDto(_aircraft1.Id, "EB1", "EBY1", _incheon.Id, departureFromIncheon, _schipol.Id, arrivalAtSchipol, 400, 4000);

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        s_dto = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken) ?? throw new JsonException();

        // Assert
        s_dto.Should().Match<FlightDto>(x =>
            x.FlightNumberIata == request.FlightNumberIata &&
            x.FlightNumberIcao == request.FlightNumberIcao &&
            x.DepartureAirportId == request.DepartureAirportId &&
            x.DepartureAirportIataCode == _incheon.IataCode &&
            x.DepartureAirportIcaoCode == _incheon.IcaoCode &&
            x.DepartureAirportName == _incheon.Name &&
            x.DepartureAirportTimeZoneId == _incheon.TimeZoneId &&
            x.ArrivalAirportId == request.ArrivalAirportId &&
            x.ArrivalAirportIataCode == _schipol.IataCode &&
            x.ArrivalAirportIcaoCode == _schipol.IcaoCode &&
            x.ArrivalAirportName == _schipol.Name &&
            x.ArrivalAirportTimeZoneId == _schipol.TimeZoneId &&
            x.DepartureLocalTime == request.DepartureLocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) &&
            x.ArrivalLocalTime == request.ArrivalLocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) &&
            x.Duration == duration &&
            x.EconomyPrice == request.EconomyPrice &&
            x.BusinessPrice == request.BusinessPrice &&
            x.AircraftId == request.AircraftId &&
            x.AircraftEquipmentCode == _aircraft1.EquipmentCode &&
            x.AircraftTailNumber == _aircraft1.TailNumber);
    }

    [Fact, TestPriority(4)]
    public async Task GetById_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = $"Flight with ID {id} not found";

        // Act
        var uri = new Uri($"flights/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(5)]
    public async Task GetById_Should_ReturnOk_WhenFlightExists()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var id = s_dto.Id;

        // Act
        var uri = new Uri($"flights/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var flightDto = await response.Content.ReadFromJsonAsync<FlightDto>(TestContext.Current.CancellationToken);

        // Assert
        flightDto.Should().BeEquivalentTo(s_dto);
    }

    [Fact, TestPriority(6)]
    public async Task AssignAircraft_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new AssignAircraftToFlightDto(Guid.NewGuid());
        var error = $"Flight with ID {id} not found";

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/aircraft", dto, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(7)]
    public async Task AssignAircraft_Should_ReturnNotFound_WhenAircraftDoesNotExist()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var dto = new AssignAircraftToFlightDto(Guid.NewGuid());
        var error = $"Aircraft with ID {dto.AircraftId} not found";

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{s_dto.Id}/aircraft", dto, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(8)]
    public async Task AssignAircraft_Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var dto = new AssignAircraftToFlightDto(_aircraft2.Id);

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{s_dto.Id}/aircraft", dto, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        s_dto = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken) ?? throw new JsonException();

        // Assert
        s_dto.AircraftId.Should().Be(_aircraft2.Id);
    }

    [Fact, TestPriority(9)]
    public async Task AdjustFlightPricing_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new AdjustFlightPricingDto(500, 5000);
        var error = $"Flight with ID {id} not found";

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/pricing", dto, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(10)]
    public async Task AdjustFlightPricing_Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var dto = new AdjustFlightPricingDto(500, 5000);

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{s_dto.Id}/pricing", dto, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        s_dto = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken) ?? throw new JsonException();

        // Assert
        s_dto.Should().Match<FlightDto>(x =>
        x.AircraftId == _aircraft2.Id &&
            x.EconomyPrice == dto.EconomyPrice &&
            x.BusinessPrice == dto.BusinessPrice);
    }

    [Fact, TestPriority(11)]
    public async Task Reschedule_Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var tomorrow = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(1));
        var departureFromIncheon = tomorrow.InZone(tz1).ToDateTimeUnspecified();
        var arrivalAtSchipol = tomorrow.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30);
        var request = new RescheduleFlightDto(departureFromIncheon, arrivalAtSchipol);

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{s_dto.Id}/schedule", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        s_dto = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken) ?? throw new JsonException();

        // Assert
        s_dto.Should().Match<FlightDto>(x =>
            x.DepartureLocalTime == request.DepartureLocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) &&
            x.ArrivalLocalTime == request.ArrivalLocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) &&
            x.EconomyPrice == 500 &&
            x.BusinessPrice == 5000 &&
            x.AircraftId == _aircraft2.Id);
    }
}
