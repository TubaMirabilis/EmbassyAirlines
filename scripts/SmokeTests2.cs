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
    AircraftId = new Guid("087163c3-d8b7-4edc-9552-e68b6367087f"),
    FlightNumberIata = "EB1",
    FlightNumberIcao = "EBY1",
    DepartureAirportId = new Guid("f7c33b38-d04d-48a8-99f0-5e943208a68f"),
    DepartureLocalTime = departureFromIncheon,
    ArrivalAirportId = new Guid("458024b2-a321-4fb1-aed0-e69cd9a468f9"),
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
