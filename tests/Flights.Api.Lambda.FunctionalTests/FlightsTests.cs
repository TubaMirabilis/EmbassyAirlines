using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Flights.Core.Models;
using Microsoft.AspNetCore.Mvc;
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
    private static readonly DateTimeZone _incheonTimeZone = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
    private static readonly DateTimeZone _schipholTimeZone = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
    private static readonly Duration _flightDuration = Duration.FromHours(10) + Duration.FromMinutes(30);
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
        var now = _clock.GetCurrentInstant();
        var departure = now.Minus(Duration.FromMinutes(30));
        var arrival = now.Plus(Duration.FromMinutes(30));
        var request = CreateValidScheduleRequest(
            departureLocalTime: departure.InZone(_incheonTimeZone).ToDateTimeUnspecified(),
            arrivalLocalTime: arrival.InZone(_schipholTimeZone).ToDateTimeUnspecified());
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(
            response,
            "Departure time cannot be in the past (Parameter 'args')");
    }

    [Fact]
    public async Task Schedule_Should_ReturnBadRequest_WhenArrivalTimeIsBeforeDepartureTime()
    {
        var now = _clock.GetCurrentInstant();
        var soon = now.Plus(Duration.FromMinutes(30));
        var request = CreateValidScheduleRequest(
            departureLocalTime: soon.InZone(_incheonTimeZone).ToDateTimeUnspecified(),
            arrivalLocalTime: now.InZone(_schipholTimeZone).ToDateTimeUnspecified());
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
        await GetProblemDetailsFromResponseAndAssert(response, "Only Delayed and Cancelled can be set directly. Record operational milestones to transition to EnRoute or Arrived.");
    }

    [Fact]
    public async Task AdjustFlightStatus_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        var id = Guid.NewGuid();
        var dto = new AdjustFlightStatusDto("Cancelled");
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/status", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Flight with ID {id} not found");
    }

    [Fact]
    public async Task RescheduleFlight_Should_ReturnBadRequest_WhenSchedulingAmbiguityPolicyIsInvalid()
    {
        var id = Guid.NewGuid();
        var dto = new RescheduleFlightDto(DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), "InvalidPolicy");
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/schedule", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, "Invalid scheduling ambiguity policy");
    }

    [Fact]
    public async Task RescheduleFlight_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        var id = Guid.NewGuid();
        var dto = new RescheduleFlightDto(DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), "ThrowWhenAmbiguous");
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/schedule", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Flight with ID {id} not found");
    }

    [Fact]
    public async Task RecordOffBlockMilestone_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        var id = Guid.NewGuid();
        var dto = new RecordFlightMilestoneDto("OffBlock", DateTimeOffset.UtcNow);
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/milestones", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, $"Flight with ID {id} not found");
    }

    [Fact]
    public async Task RecordOffBlockMilestone_Should_ReturnBadRequest_WhenFutureToleranceLimitIsViolated()
    {
        var id = Guid.NewGuid();
        var dto = new RecordFlightMilestoneDto("OffBlock", DateTimeOffset.UtcNow.AddMinutes(15));
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/milestones", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, "The request submitted a timestamp which violates the future tolerance limit.");
    }

    [Fact]
    public async Task Flight_Lifecycle_Should_Succeed()
    {
        await VerifyFlightListIsEmptyAsync();
        var flight = await ScheduleFlightAsync();
        await VerifyFlightIsListedAsync(flight);
        await VerifyFilteredListContainsFlightAsync(flight);
        await VerifyExcessPageReturnsLastPageAsync(flight);
        await VerifyFlightCanBeRetrievedByIdAsync(flight);
        flight = await AssignAircraftAsync(flight, _aircraft2);
        flight = await AdjustPricingAsync(flight, new AdjustFlightPricingDto(500, 5000));
        var dto = new AdjustFlightPricingDto(-500, 5000);
        var response = await HttpClient.PatchAsJsonAsync($"flights/{flight.Id}/pricing", dto, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response, "Value must be greater than or equal to zero. (Parameter 'amount')");
        var departureInstant = TimeHelpers.MinutesFromNowRoundedUp(_clock, 1).Minus(Duration.FromHours(1));
        var arrivalInstant = departureInstant + _flightDuration;
        var departureZoned = departureInstant.InZone(_incheonTimeZone);
        var arrivalZoned = arrivalInstant.InZone(_schipholTimeZone);
        var dto2 = new RescheduleFlightDto(departureZoned.ToDateTimeUnspecified(), arrivalZoned.ToDateTimeUnspecified(), "ThrowWhenAmbiguous");
        var response2 = await HttpClient.PatchAsJsonAsync($"flights/{flight.Id}/schedule", dto2, TestContext.Current.CancellationToken);
        await GetProblemDetailsFromResponseAndAssert(response2, "Departure time cannot be in the past (Parameter 'args')");
        await RescheduleFlightAsync(flight);
        await RecordOffBlockMilestoneAsync(flight);
    }

    private async Task VerifyFlightListIsEmptyAsync()
    {
        var flights = await ListFlightsAsync(new Uri("flights", UriKind.Relative));
        flights.Should().BeEquivalentTo(new FlightListDto([], 1, 50, 0, false));
    }

    private async Task<FlightDto> ScheduleFlightAsync()
    {
        var departureInstant = TimeHelpers.MinutesFromNowRoundedUp(_clock, 30);
        var arrivalInstant = departureInstant + _flightDuration;
        var departureZoned = departureInstant.InZone(_incheonTimeZone);
        var arrivalZoned = arrivalInstant.InZone(_schipholTimeZone);
        var request = CreateValidScheduleRequest(
            departureLocalTime: departureZoned.ToDateTimeUnspecified(),
            arrivalLocalTime: arrivalZoned.ToDateTimeUnspecified());
        var duration = _flightDuration.ToTimeSpan();
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await DeserializeAsync<ProblemDetails>(response);
            throw new HttpRequestException($"Scheduling flight failed with status code {response.StatusCode}: {problemDetails.Detail}");
        }
        var flight = await DeserializeAsync<FlightDto>(response);
        flight.Should().Match<FlightDto>(x =>
            x.FlightNumberIata == request.FlightNumberIata &&
            x.FlightNumberIcao == request.FlightNumberIcao &&
            x.DepartureAirportId == request.DepartureAirportId &&
            x.DepartureAirportIataCode == _incheon.IataCode &&
            x.DepartureAirportIcaoCode == _incheon.IcaoCode &&
            x.DepartureAirportName == _incheon.Name &&
            x.DepartureAirportTimeZoneId == _incheon.TimeZoneId &&
            x.ArrivalAirportId == request.ArrivalAirportId &&
            x.ArrivalAirportIataCode == _schiphol.IataCode &&
            x.ArrivalAirportIcaoCode == _schiphol.IcaoCode &&
            x.ArrivalAirportName == _schiphol.Name &&
            x.ArrivalAirportTimeZoneId == _schiphol.TimeZoneId &&
            x.DepartureTime == departureZoned.ToDateTimeOffset() &&
            x.ArrivalTime == arrivalZoned.ToDateTimeOffset() &&
            x.Duration == duration &&
            x.EconomyPrice == request.EconomyPrice &&
            x.BusinessPrice == request.BusinessPrice &&
            x.AircraftId == request.AircraftId &&
            x.AircraftEquipmentCode == _aircraft1.EquipmentCode &&
            x.Status == "Scheduled" &&
            x.AircraftTailNumber == _aircraft1.TailNumber);
        return flight;
    }

    private async Task VerifyFlightIsListedAsync(FlightDto flight)
    {
        var flights = await ListFlightsAsync(new Uri("flights", UriKind.Relative));
        flights.Should().BeEquivalentTo(new FlightListDto([flight], 1, 50, 1, false));
    }

    private async Task VerifyFilteredListContainsFlightAsync(FlightDto flight)
    {
        var uri = new Uri($"flights?from={_incheon.IataCode}&to={_schiphol.IataCode}&page=1&pageSize=50", UriKind.Relative);
        var flights = await ListFlightsAsync(uri);
        flights.Should().BeEquivalentTo(new FlightListDto([flight], 1, 50, 1, false));
    }

    // Requesting a page beyond the last one should return the last page
    private async Task VerifyExcessPageReturnsLastPageAsync(FlightDto flight)
    {
        var uri = new Uri($"flights?from={_incheon.IataCode}&to={_schiphol.IataCode}&page=2&pageSize=50", UriKind.Relative);
        var flights = await ListFlightsAsync(uri);
        flights.Should().BeEquivalentTo(new FlightListDto([flight], 1, 50, 1, false));
    }

    private async Task VerifyFlightCanBeRetrievedByIdAsync(FlightDto flight)
    {
        var uri = new Uri($"flights/{flight.Id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var fetchedFlight = await response.Content.ReadFromJsonAsync<FlightDto>(TestContext.Current.CancellationToken);
        fetchedFlight.Should().BeEquivalentTo(flight);
    }

    private async Task<FlightDto> AssignAircraftAsync(FlightDto flight, Aircraft aircraft)
    {
        var request = new AssignAircraftToFlightDto(aircraft.Id);
        var response = await HttpClient.PatchAsJsonAsync(
            $"flights/{flight.Id}/aircraft",
            request,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var updatedFlight = await DeserializeAsync<FlightDto>(response);
        updatedFlight.AircraftId.Should().Be(aircraft.Id);
        return updatedFlight;
    }

    private async Task<FlightDto> AdjustPricingAsync(FlightDto flight, AdjustFlightPricingDto request)
    {
        var response = await HttpClient.PatchAsJsonAsync(
            $"flights/{flight.Id}/pricing",
            request,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var updatedFlight = await DeserializeAsync<FlightDto>(response);
        updatedFlight.Should().Match<FlightDto>(x =>
            x.AircraftId == flight.AircraftId &&
            x.Status == "Scheduled" &&
            x.EconomyPrice == request.EconomyPrice &&
            x.BusinessPrice == request.BusinessPrice);
        return updatedFlight;
    }

    private async Task RescheduleFlightAsync(FlightDto flight)
    {
        var departureInstant = TimeHelpers.MinutesFromNowRoundedUp(_clock, 35);
        var arrivalInstant = departureInstant + _flightDuration;
        var departureZoned = departureInstant.InZone(_incheonTimeZone);
        var arrivalZoned = arrivalInstant.InZone(_schipholTimeZone);
        var request = new RescheduleFlightDto(departureZoned.ToDateTimeUnspecified(), arrivalZoned.ToDateTimeUnspecified(), "ThrowWhenAmbiguous");
        var response = await HttpClient.PatchAsJsonAsync($"flights/{flight.Id}/schedule", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var rescheduledFlight = await DeserializeAsync<FlightDto>(response);
        rescheduledFlight.Should().Match<FlightDto>(x =>
            x.DepartureTime == departureZoned.ToDateTimeOffset() &&
            x.ArrivalTime == arrivalZoned.ToDateTimeOffset() &&
            x.Status == "Scheduled" &&
            x.EconomyPrice == flight.EconomyPrice &&
            x.BusinessPrice == flight.BusinessPrice &&
            x.AircraftId == flight.AircraftId);
    }

    private async Task RecordOffBlockMilestoneAsync(FlightDto flight)
    {
        var request = new RecordFlightMilestoneDto("OffBlock", DateTimeOffset.UtcNow);
        var response = await HttpClient.PatchAsJsonAsync($"flights/{flight.Id}/milestones", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var enRouteFlight = await DeserializeAsync<FlightDto>(response);
        enRouteFlight.Status.Should().BeEquivalentTo("EnRoute");
    }

    private async Task<FlightListDto?> ListFlightsAsync(Uri uri)
    {
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FlightListDto>(TestContext.Current.CancellationToken);
    }

    private ScheduleFlightDto CreateValidScheduleRequest(
        Guid? aircraftId = null,
        Guid? departureAirportId = null,
        Guid? arrivalAirportId = null,
        DateTime? departureLocalTime = null,
        DateTime? arrivalLocalTime = null,
        string schedulingAmbiguityPolicy = "ThrowWhenAmbiguous")
    {
        var flightDuration = Duration.FromHours(10) + Duration.FromMinutes(30);
        var departureInstant = TimeHelpers.MinutesFromNowRoundedUp(_clock, 30);
        var arrivalInstant = departureInstant + flightDuration;
        return new ScheduleFlightDto
        {
            AircraftId = aircraftId ?? _aircraft1.Id,
            FlightNumberIata = "EB1",
            FlightNumberIcao = "EBY1",
            DepartureAirportId = departureAirportId ?? _incheon.Id,
            DepartureLocalTime = departureLocalTime ?? departureInstant.InZone(_incheonTimeZone).ToDateTimeUnspecified(),
            ArrivalAirportId = arrivalAirportId ?? _schiphol.Id,
            ArrivalLocalTime = arrivalLocalTime ?? arrivalInstant.InZone(_schipholTimeZone).ToDateTimeUnspecified(),
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
