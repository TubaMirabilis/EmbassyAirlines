using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Flights.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Shared.Contracts;

namespace Flights.Api.Lambda.FunctionalTests;

public class AdjustFlightStatusTests : BaseFunctionalTest
{
    private readonly Airport _incheon;
    private readonly Airport _schiphol;
    private readonly Aircraft _aircraft1;
    private readonly IClock _clock;
    private static readonly DateTimeZone _incheonTimeZone = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
    private static readonly DateTimeZone _schipholTimeZone = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
    private static readonly Duration _flightDuration = Duration.FromHours(10) + Duration.FromMinutes(30);
    public AdjustFlightStatusTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        ArgumentNullException.ThrowIfNull(factory.IncheonAirport);
        ArgumentNullException.ThrowIfNull(factory.SchipholAirport);
        ArgumentNullException.ThrowIfNull(factory.Aircraft1);
        ArgumentNullException.ThrowIfNull(factory.Aircraft2);
        _incheon = factory.IncheonAirport;
        _schiphol = factory.SchipholAirport;
        _aircraft1 = factory.Aircraft1;
        _clock = factory.Services.GetRequiredService<IClock>();
    }

    [Fact]
    public async Task AdjustFlightStatus_Should_ReturnOk_WhenFlightStatusIsAdjustedRepeatedly()
    {
        var flight = await ScheduleFlightAsync();
        var id = flight.Id;
        var dto = new AdjustFlightStatusDto("Delayed");
        var response = await HttpClient.PatchAsJsonAsync($"flights/{id}/status", dto, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var flight2 = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        if (flight2 is null)
        {
            throw new JsonException("Deserialized aircraft is null");
        }
        flight2.Status.Should().BeEquivalentTo("Delayed");
        var dto2 = new AdjustFlightStatusDto("Cancelled");
        var response2 = await HttpClient.PatchAsJsonAsync($"flights/{id}/status", dto2, TestContext.Current.CancellationToken);
        var content2 = await response2.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var flight3 = await JsonSerializer.DeserializeAsync<FlightDto>(content2, JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
        if (flight3 is null)
        {
            throw new JsonException("Deserialized aircraft is null");
        }
        flight3.Status.Should().BeEquivalentTo("Cancelled");
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
    private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var result = await JsonSerializer.DeserializeAsync<T>(
            stream,
            JsonSerializerOptions.Web,
            TestContext.Current.CancellationToken);
        return result ?? throw new JsonException();
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
}
