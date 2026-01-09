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
    AircraftId = new Guid("467c3af8-9ae9-40b0-b7aa-cdddc63e80d1"),
    FlightNumberIata = "EB1",
    FlightNumberIcao = "EBY1",
    DepartureAirportId = new Guid("2d25f099-7c58-4159-9574-108e21870efa"),
    DepartureLocalTime = departureFromIncheon,
    ArrivalAirportId = new Guid("fe3254e6-b0d1-43f7-b2ef-2c61953bb95c"),
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
