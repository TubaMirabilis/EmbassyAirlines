#:project ../src/Shared
#:package AWSSDK.S3
#:package CliWrap
#:package NodaTime
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using Shared.Contracts;
using SmokeTests;

var baseAddress = args.Length > 0 ? args[0] : "https://embassyairlines.com/api/";
using var client = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};
var tz1 = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
var tz2 = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
var soon = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(30));
soon = Instant.FromUnixTimeTicks((soon.ToUnixTimeTicks() / NodaConstants.TicksPerMinute + 1) * NodaConstants.TicksPerMinute);
var departureFromIncheon = soon.InZone(tz1).ToDateTimeUnspecified();
var arrivalAtSchipol = soon.InZone(tz2).ToDateTimeUnspecified().AddHours(10).AddMinutes(30);
var req4 = new ScheduleFlightDto
{
    AircraftId = new Guid("23167325-0acb-422e-a679-6e7e23b9ac51"),
    FlightNumberIata = "EB1",
    FlightNumberIcao = "EBY1",
    DepartureAirportId = new Guid("21217064-fba2-4295-90ff-962b5a8def7a"),
    DepartureLocalTime = departureFromIncheon,
    ArrivalAirportId = new Guid("dffb2ee4-c81e-42fc-8af8-b355d9d379db"),
    ArrivalLocalTime = arrivalAtSchipol,
    EconomyPrice = 400,
    BusinessPrice = 4000,
    SchedulingAmbiguityPolicy = "ThrowWhenAmbiguous",
    OperationType = "RevenuePassenger"
};
var res = await client.PostAsJsonAsync("flights", req4, SmokeTestJsonContext.Default.ScheduleFlightDto);
res.EnsureSuccessStatusCode();
var stream4 = await res.Content.ReadAsStreamAsync();
var flight = await JsonSerializer.DeserializeAsync<FlightDto>(stream4, SmokeTestJsonContext.Default.FlightDto) ?? throw new JsonException("Deserialization returned null");
Console.WriteLine($"Scheduled flight with ID: {flight.Id}");

namespace SmokeTests
{
    [JsonSerializable(typeof(FlightDto))]
    [JsonSerializable(typeof(ScheduleFlightDto))]
    internal sealed partial class SmokeTestJsonContext : JsonSerializerContext
    {
    }
}
