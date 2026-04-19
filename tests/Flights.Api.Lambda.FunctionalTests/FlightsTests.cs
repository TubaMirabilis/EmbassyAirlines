using System.Net.Http.Json;
using System.Text.Json;
using Flights.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Shared.Contracts;

namespace Flights.Api.Lambda.FunctionalTests;

public class FlightsTests : BaseFunctionalTest
{
    private readonly Airport _incheon;
    private readonly Airport _schiphol;
    private readonly Aircraft _aircraft1;
    private readonly Aircraft _aircraft2;
    private readonly IClock _clock;
    public FlightsTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        ArgumentNullException.ThrowIfNull(factory.IncheonAirport);
        ArgumentNullException.ThrowIfNull(factory.SchipholAirport);
        ArgumentNullException.ThrowIfNull(factory.Aircraft1);
        ArgumentNullException.ThrowIfNull(factory.Aircraft2);
        _incheon = factory.IncheonAirport;
        _schiphol = factory.SchipholAirport;
        _aircraft1 = factory.Aircraft1;
        _aircraft2 = factory.Aircraft2;
        _clock = factory.Services.GetRequiredService<IClock>();
    }

    [Fact]
    public async Task Schedule_Should_ReturnBadRequest_WhenDepartureTimeIsInThePast()
    {
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var recently = _clock.GetCurrentInstant().Minus(Duration.FromMinutes(30));
        var soon = _clock.GetCurrentInstant().Plus(Duration.FromMinutes(30));
        var request = CreateValidScheduleRequest(
            departureLocalTime: recently.InZone(tz1).ToDateTimeUnspecified(),
            arrivalLocalTime: soon.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30));
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(
            response,
            "Departure time cannot be in the past (Parameter 'args')");
    }

    [Fact]
    public async Task Schedule_Should_ReturnBadRequest_WhenArrivalTimeIsBeforeDepartureTime()
    {
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var now = _clock.GetCurrentInstant();
        var soon = now.Plus(Duration.FromMinutes(30));
        var request = CreateValidScheduleRequest(
            departureLocalTime: soon.InZone(tz1).ToDateTimeUnspecified(),
            arrivalLocalTime: now.InZone(tz2).ToDateTimeUnspecified());
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(
            response,
            "Arrival time cannot be before departure time (Parameter 'args')");
    }

    [Fact]
    public async Task Schedule_Should_ReturnNotFound_WhenAircraftDoesNotExist()
    {
        var id = Guid.NewGuid();
        var request = CreateValidScheduleRequest(aircraftId: id);
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Aircraft with ID {id} not found");
    }

    [Fact]
    public async Task Schedule_Should_ReturnNotFound_WhenDepartureAirportDoesNotExist()
    {
        var id = Guid.NewGuid();
        var request = CreateValidScheduleRequest(departureAirportId: id);
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Departure airport with ID {id} not found");
    }

    [Fact]
    public async Task Schedule_Should_ReturnNotFound_WhenArrivalAirportDoesNotExist()
    {
        var id = Guid.NewGuid();
        var request = CreateValidScheduleRequest(arrivalAirportId: id);
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Arrival airport with ID {id} not found");
    }

    [Fact]
    public async Task Schedule_Should_ReturnBadRequest_WhenSchedulingAmbiguityPolicyIsInvalid()
    {
        var request = CreateValidScheduleRequest(schedulingAmbiguityPolicy: "None");
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, "Invalid scheduling ambiguity policy: None");
    }

    [Fact]
    public async Task GetById_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        var id = Guid.NewGuid();
        var uri = new Uri($"flights/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Flight with ID {id} not found");
    }

    [Fact]
    public async Task AssignAircraft_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        var id = Guid.NewGuid();
        var dto = new AssignAircraftToFlightDto(Guid.NewGuid());
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/aircraft", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Flight with ID {id} not found");
    }

    [Fact]
    public async Task AdjustFlightPricing_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        var id = Guid.NewGuid();
        var dto = new AdjustFlightPricingDto(500, 5000);
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/pricing", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Flight with ID {id} not found");
    }

    [Fact]
    public async Task AdjustFlightStatus_Should_ReturnBadRequest_WhenFlightStatusIsInvalid()
    {
        var id = Guid.NewGuid();
        var dto = new AdjustFlightStatusDto("Asdf");
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/status", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, "Invalid flight status: Asdf");
    }

    [Fact]
    public async Task AdjustFlightStatus_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        var id = Guid.NewGuid();
        var dto = new AdjustFlightStatusDto("EnRoute");
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/status", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Flight with ID {id} not found");
    }

    [Fact]
    public async Task Flight_Lifecycle_Should_Succeed()
    {
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var flightDuration = Duration.FromHours(10) + Duration.FromMinutes(30);
        var departureInstant = TimeHelpers.MinutesFromNowRoundedUp(_clock, 30);
        var arrivalInstant = departureInstant + flightDuration;
        var departureZoned = departureInstant.InZone(tz1);
        var arrivalZoned = arrivalInstant.InZone(tz2);
        var scheduleRequest = CreateValidScheduleRequest(
            departureLocalTime: departureZoned.ToDateTimeUnspecified(),
            arrivalLocalTime: arrivalZoned.ToDateTimeUnspecified());
        var duration = flightDuration.ToTimeSpan();

        // Schedule
        var scheduleResponse = await HttpClient.PostAsJsonAsync("flights", scheduleRequest, TestContext.Current.CancellationToken);
        scheduleResponse.EnsureSuccessStatusCode();
        var flight = await DeserializeAsync<FlightDto>(scheduleResponse);
        flight.Should().Match<FlightDto>(x =>
            x.FlightNumberIata == scheduleRequest.FlightNumberIata &&
            x.FlightNumberIcao == scheduleRequest.FlightNumberIcao &&
            x.DepartureAirportId == scheduleRequest.DepartureAirportId &&
            x.DepartureAirportIataCode == _incheon.IataCode &&
            x.DepartureAirportIcaoCode == _incheon.IcaoCode &&
            x.DepartureAirportName == _incheon.Name &&
            x.DepartureAirportTimeZoneId == _incheon.TimeZoneId &&
            x.ArrivalAirportId == scheduleRequest.ArrivalAirportId &&
            x.ArrivalAirportIataCode == _schiphol.IataCode &&
            x.ArrivalAirportIcaoCode == _schiphol.IcaoCode &&
            x.ArrivalAirportName == _schiphol.Name &&
            x.ArrivalAirportTimeZoneId == _schiphol.TimeZoneId &&
            x.DepartureTime == departureZoned.ToDateTimeOffset() &&
            x.ArrivalTime == arrivalZoned.ToDateTimeOffset() &&
            x.Duration == duration &&
            x.EconomyPrice == scheduleRequest.EconomyPrice &&
            x.BusinessPrice == scheduleRequest.BusinessPrice &&
            x.AircraftId == scheduleRequest.AircraftId &&
            x.AircraftEquipmentCode == _aircraft1.EquipmentCode &&
            x.Status == "Scheduled" &&
            x.AircraftTailNumber == _aircraft1.TailNumber);

        // List
        var uri = new Uri("flights", UriKind.Relative);
        var listResponse = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        listResponse.EnsureSuccessStatusCode();
        var listedFlights = await listResponse.Content.ReadFromJsonAsync<FlightListDto>(TestContext.Current.CancellationToken);
        listedFlights.Should().BeEquivalentTo(new FlightListDto([flight], 1, 50, 1, false));

        // List with filters
        var uriWithFilters = new Uri($"flights?from={_incheon.IataCode}&to={_schiphol.IataCode}&page=1&pageSize=50", UriKind.Relative);
        var filteredListResponse = await HttpClient.GetAsync(uriWithFilters, TestContext.Current.CancellationToken);
        filteredListResponse.EnsureSuccessStatusCode();
        var filteredFlights = await filteredListResponse.Content.ReadFromJsonAsync<FlightListDto>(TestContext.Current.CancellationToken);
        filteredFlights.Should().BeEquivalentTo(new FlightListDto([flight], 1, 50, 1, false));

        // Get by id
        var getByIdUri = new Uri($"flights/{flight.Id}", UriKind.Relative);
        var getByIdResponse = await HttpClient.GetAsync(getByIdUri, TestContext.Current.CancellationToken);
        getByIdResponse.EnsureSuccessStatusCode();
        var fetchedFlight = await getByIdResponse.Content.ReadFromJsonAsync<FlightDto>(TestContext.Current.CancellationToken);
        fetchedFlight.Should().BeEquivalentTo(flight);

        // Assign aircraft
        var assignAircraftRequest = new AssignAircraftToFlightDto(_aircraft2.Id);
        var assignAircraftResponse = await HttpClient.PatchAsJsonAsync(
            $"flights/{flight.Id}/aircraft",
            assignAircraftRequest,
            TestContext.Current.CancellationToken);
        assignAircraftResponse.EnsureSuccessStatusCode();
        flight = await DeserializeAsync<FlightDto>(assignAircraftResponse);
        flight.AircraftId.Should().Be(_aircraft2.Id);

        // Adjust pricing
        var adjustPricingRequest = new AdjustFlightPricingDto(500, 5000);
        var adjustPricingResponse = await HttpClient.PatchAsJsonAsync(
            $"flights/{flight.Id}/pricing",
            adjustPricingRequest,
            TestContext.Current.CancellationToken);
        adjustPricingResponse.EnsureSuccessStatusCode();
        flight = await DeserializeAsync<FlightDto>(adjustPricingResponse);
        flight.Should().Match<FlightDto>(x =>
            x.AircraftId == _aircraft2.Id &&
            x.Status == "Scheduled" &&
            x.EconomyPrice == adjustPricingRequest.EconomyPrice &&
            x.BusinessPrice == adjustPricingRequest.BusinessPrice);

        // Reschedule
        var tomorrow = _clock.GetCurrentInstant().Plus(Duration.FromDays(1));
        var rescheduleRequest = new RescheduleFlightDto(
            tomorrow.InZone(tz1).ToDateTimeUnspecified(),
            tomorrow.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30),
            "ThrowWhenAmbiguous");
        var rescheduleResponse = await HttpClient.PatchAsJsonAsync(
            $"flights/{flight.Id}/schedule",
            rescheduleRequest,
            TestContext.Current.CancellationToken);
        rescheduleResponse.EnsureSuccessStatusCode();
        flight = await DeserializeAsync<FlightDto>(rescheduleResponse);
        flight.Should().Match<FlightDto>(x =>
            x.DepartureTime == tomorrow.InZone(tz1).ToDateTimeOffset() &&
            x.ArrivalTime == tomorrow.InZone(tz2).ToDateTimeOffset().AddHours(10).AddMinutes(30) &&
            x.Status == "Scheduled" &&
            x.EconomyPrice == 500 &&
            x.BusinessPrice == 5000 &&
            x.AircraftId == _aircraft2.Id);

        // Adjust status
        var adjustStatusRequest = new AdjustFlightStatusDto("EnRoute");
        var adjustStatusResponse = await HttpClient.PatchAsJsonAsync(
            $"flights/{flight.Id}/status",
            adjustStatusRequest,
            TestContext.Current.CancellationToken);
        adjustStatusResponse.EnsureSuccessStatusCode();
        flight = await DeserializeAsync<FlightDto>(adjustStatusResponse);
        flight.Should().Match<FlightDto>(x =>
            x.AircraftId == _aircraft2.Id &&
            x.Status == "EnRoute");
    }

    private ScheduleFlightDto CreateValidScheduleRequest(
        Guid? aircraftId = null,
        Guid? departureAirportId = null,
        Guid? arrivalAirportId = null,
        DateTime? departureLocalTime = null,
        DateTime? arrivalLocalTime = null,
        string schedulingAmbiguityPolicy = "ThrowWhenAmbiguous")
    {
        var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
        var flightDuration = Duration.FromHours(10) + Duration.FromMinutes(30);
        var departureInstant = TimeHelpers.MinutesFromNowRoundedUp(_clock, 30);
        var arrivalInstant = departureInstant + flightDuration;
        return new ScheduleFlightDto
        {
            AircraftId = aircraftId ?? _aircraft1.Id,
            FlightNumberIata = "EB1",
            FlightNumberIcao = "EBY1",
            DepartureAirportId = departureAirportId ?? _incheon.Id,
            DepartureLocalTime = departureLocalTime ?? departureInstant.InZone(tz1).ToDateTimeUnspecified(),
            ArrivalAirportId = arrivalAirportId ?? _schiphol.Id,
            ArrivalLocalTime = arrivalLocalTime ?? arrivalInstant.InZone(tz2).ToDateTimeUnspecified(),
            EconomyPrice = 400,
            BusinessPrice = 4000,
            SchedulingAmbiguityPolicy = schedulingAmbiguityPolicy,
            OperationType = "RevenuePassenger"
        };
    }

    private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var result = await JsonSerializer.DeserializeAsync<T>(
            stream,
            JsonSerializerOptions.Web,
            TestContext.Current.CancellationToken);
        return result ?? throw new JsonException();
    }
}
