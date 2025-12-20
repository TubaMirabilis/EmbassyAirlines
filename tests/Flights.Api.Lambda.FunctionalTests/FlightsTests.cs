using System.Net.Http.Json;
using System.Text.Json;
using Flights.Core.Models;
using FluentAssertions;
using NodaTime;
using Shared.Contracts;

namespace Flights.Api.Lambda.FunctionalTests;

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
        var request = new CreateOrUpdateFlightDto
        {
            AircraftId = _aircraft1.Id,
            FlightNumberIata = "EB1",
            FlightNumberIcao = "EBY1",
            DepartureAirportId = _incheon.Id,
            DepartureLocalTime = departureFromIncheon,
            ArrivalAirportId = _schipol.Id,
            ArrivalLocalTime = arrivalAtSchipol,
            EconomyPrice = 400,
            BusinessPrice = 4000,
            SchedulingAmbiguityPolicy = "ThrowWhenAmbiguous"
        };
        var error = "Departure time cannot be in the past (Parameter 'args')";

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
        var request = new CreateOrUpdateFlightDto
        {
            AircraftId = _aircraft1.Id,
            FlightNumberIata = "EB1",
            FlightNumberIcao = "EBY1",
            DepartureAirportId = _incheon.Id,
            DepartureLocalTime = departureFromIncheon,
            ArrivalAirportId = _schipol.Id,
            ArrivalLocalTime = arrivalAtSchipol,
            EconomyPrice = 400,
            BusinessPrice = 4000,
            SchedulingAmbiguityPolicy = "ThrowWhenAmbiguous"
        };
        var error = "Arrival time cannot be before departure time (Parameter 'args')";

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
        var request = new CreateOrUpdateFlightDto
        {
            AircraftId = id,
            FlightNumberIata = "EB1",
            FlightNumberIcao = "EBY1",
            DepartureAirportId = _incheon.Id,
            DepartureLocalTime = departureFromIncheon,
            ArrivalAirportId = _schipol.Id,
            ArrivalLocalTime = arrivalAtSchipol,
            EconomyPrice = 400,
            BusinessPrice = 4000,
            SchedulingAmbiguityPolicy = "ThrowWhenAmbiguous"
        };
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
        soon = Instant.FromUnixTimeTicks((soon.ToUnixTimeTicks() / NodaConstants.TicksPerMinute + 1) * NodaConstants.TicksPerMinute);
        var departureFromIncheon = soon.InZone(tz1).ToDateTimeUnspecified();
        var arrivalAtSchipol = soon.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30);
        var duration = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30));
        var request = new CreateOrUpdateFlightDto
        {
            AircraftId = _aircraft1.Id,
            FlightNumberIata = "EB1",
            FlightNumberIcao = "EBY1",
            DepartureAirportId = _incheon.Id,
            DepartureLocalTime = departureFromIncheon,
            ArrivalAirportId = _schipol.Id,
            ArrivalLocalTime = arrivalAtSchipol,
            EconomyPrice = 400,
            BusinessPrice = 4000,
            SchedulingAmbiguityPolicy = "ThrowWhenAmbiguous"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var flight = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken);
        if (flight is null)
        {
            throw new JsonException();
        }
        s_dto = flight;

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
            x.DepartureTime == soon.InZone(tz1).ToDateTimeOffset() &&
            x.ArrivalTime == soon.InZone(tz2).ToDateTimeOffset().AddHours(10).AddMinutes(30) &&
            x.Duration == duration &&
            x.EconomyPrice == request.EconomyPrice &&
            x.BusinessPrice == request.BusinessPrice &&
            x.AircraftId == request.AircraftId &&
            x.AircraftEquipmentCode == _aircraft1.EquipmentCode &&
            x.AircraftTailNumber == _aircraft1.TailNumber);
    }

    [Fact, TestPriority(4)]
    public async Task List_Should_ReturnOk_WhenFlightsExist()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var expected = new FlightListDto([s_dto], 1, 50, 1, false);

        // Act
        var uri = new Uri("flights", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var flightDtos = await response.Content.ReadFromJsonAsync<FlightListDto>(TestContext.Current.CancellationToken);

        // Assert
        flightDtos.Should().BeEquivalentTo(expected);
    }

    [Fact, TestPriority(5)]
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

    [Fact, TestPriority(6)]
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

    [Fact, TestPriority(7)]
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

    [Fact, TestPriority(8)]
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

    [Fact, TestPriority(9)]
    public async Task AssignAircraft_Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var dto = new AssignAircraftToFlightDto(_aircraft2.Id);

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{s_dto.Id}/aircraft", dto, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var flight = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken);
        if (flight is null)
        {
            throw new JsonException();
        }
        s_dto = flight;

        // Assert
        s_dto.AircraftId.Should().Be(_aircraft2.Id);
    }

    [Fact, TestPriority(10)]
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

    [Fact, TestPriority(11)]
    public async Task AdjustFlightPricing_Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var dto = new AdjustFlightPricingDto(500, 5000);

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{s_dto.Id}/pricing", dto, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var flight = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken);
        if (flight is null)
        {
            throw new JsonException();
        }
        s_dto = flight;

        // Assert
        s_dto.Should().Match<FlightDto>(x =>
        x.AircraftId == _aircraft2.Id &&
            x.EconomyPrice == dto.EconomyPrice &&
            x.BusinessPrice == dto.BusinessPrice);
    }

    [Fact, TestPriority(12)]
    public async Task Reschedule_Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(s_dto);
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var tomorrow = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(1));
        var departureFromIncheon = tomorrow.InZone(tz1).ToDateTimeUnspecified();
        var arrivalAtSchipol = tomorrow.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30);
        var request = new RescheduleFlightDto(departureFromIncheon, arrivalAtSchipol, "ThrowWhenAmbiguous");

        // Act
        var response = await HttpClient.PatchAsJsonAsync($"flights/{s_dto.Id}/schedule", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var flight = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken);
        if (flight is null)
        {
            throw new JsonException();
        }
        s_dto = flight;

        // Assert
        s_dto.Should().Match<FlightDto>(x =>
            x.DepartureTime == tomorrow.InZone(tz1).ToDateTimeOffset() &&
            x.ArrivalTime == tomorrow.InZone(tz2).ToDateTimeOffset().AddHours(10).AddMinutes(30) &&
            x.EconomyPrice == 500 &&
            x.BusinessPrice == 5000 &&
            x.AircraftId == _aircraft2.Id);
    }
}
