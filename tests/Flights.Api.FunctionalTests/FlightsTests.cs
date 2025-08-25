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
    private static FlightDto? _dto;
    private readonly Airport _incheon;
    private readonly Airport _schipol;
    private readonly Aircraft _aircraft;
    public FlightsTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        ArgumentNullException.ThrowIfNull(factory.IncheonAirport);
        ArgumentNullException.ThrowIfNull(factory.SchipolAirport);
        ArgumentNullException.ThrowIfNull(factory.Aircraft);
        _incheon = factory.IncheonAirport;
        _schipol = factory.SchipolAirport;
        _aircraft = factory.Aircraft;
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
        var request = new CreateOrUpdateFlightDto(_aircraft.Id, "EB1", "EBY1", _incheon.Id, departureFromIncheon, _schipol.Id, arrivalAtSchipol, 400, 4000);
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
        var request = new CreateOrUpdateFlightDto(_aircraft.Id, "EB1", "EBY1", _incheon.Id, departureFromIncheon, _schipol.Id, arrivalAtSchipol, 400, 4000);
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
        var request = new CreateOrUpdateFlightDto(_aircraft.Id, "EB1", "EBY1", _incheon.Id, departureFromIncheon, _schipol.Id, arrivalAtSchipol, 400, 4000);

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        _dto = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken) ?? throw new JsonException();

        // Assert
        _dto.Should().Match<FlightDto>(x =>
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
            x.AircraftEquipmentCode == _aircraft.EquipmentCode &&
            x.AircraftTailNumber == _aircraft.TailNumber);
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
        ArgumentNullException.ThrowIfNull(_dto);
        var id = _dto.Id;

        // Act
        var uri = new Uri($"flights/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var flightDto = await response.Content.ReadFromJsonAsync<FlightDto>(TestContext.Current.CancellationToken);

        // Assert
        flightDto.Should().BeEquivalentTo(_dto);
    }
}
