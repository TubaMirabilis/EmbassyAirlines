using NodaTime;
using Shared.Contracts;
using SmokeTests;

var baseAddress = args.Length > 0 ? args[0] : "https://embassyairlines.com/api/";
using var client = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};
await AirportsSmokeTestActions.ReadyAsync(client);
var req1 = new CreateOrUpdateAirportDto("RKSI", "ICN", "Incheon International Airport", "Asia/Seoul");
var req2 = new CreateOrUpdateAirportDto("EHAM", "AMS", "Schiphol Airport", "Europe/Amsterdam");
var airport1 = await AirportsSmokeTestActions.AttemptPostAsync(client, req1);
var airport2 = await AirportsSmokeTestActions.AttemptPostAsync(client, req2);
await AircraftSmokeTestActions.ReadyAsync(client);
await AircraftSmokeTestActions.PrepareS3Async("Resources/Layouts/B78X.json", "seat-layouts/B78X.json");
var req3 = new CreateAircraftDto("PH-JRN", "B78X", 135500, "Parked", 254011, "RKSI", null, 201848, 192777, 101522);
var aircraft = await AircraftSmokeTestActions.AttemptPostAsync(client, req3);
await FlightsSmokeTestActions.ReadyAsync(client);
var departureInstant = NextMinuteBoundaryAfter(Duration.FromMinutes(30));
var flightDuration = Duration.FromHours(10) + Duration.FromMinutes(30);
var arrivalInstant = departureInstant + flightDuration;
var incheonTimeZone = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
var schipholTimeZone = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
var departureZoned = departureInstant.InZone(incheonTimeZone);
var arrivalZoned = arrivalInstant.InZone(schipholTimeZone);
var req4 = new ScheduleFlightDto
{
    AircraftId = aircraft.Id,
    FlightNumberIata = "EB1",
    FlightNumberIcao = "EBY1",
    DepartureAirportId = airport1.Id,
    DepartureLocalTime = departureZoned.ToDateTimeUnspecified(),
    ArrivalAirportId = airport2.Id,
    ArrivalLocalTime = arrivalZoned.ToDateTimeUnspecified(),
    EconomyPrice = 400,
    BusinessPrice = 4000,
    SchedulingAmbiguityPolicy = "ThrowWhenAmbiguous",
    OperationType = "RevenuePassenger"
};
await FlightsSmokeTestActions.AttemptPostAsync(client, req4);
Instant NextMinuteBoundaryAfter(Duration duration)
{
    var clock = SystemClock.Instance;
    var target = clock.GetCurrentInstant() + duration;
    return AdvanceToNextMinuteBoundary(target);
}
Instant AdvanceToNextMinuteBoundary(Instant instant)
{
    var ticks = instant.ToUnixTimeTicks();
    var ticksPerMinute = NodaConstants.TicksPerMinute;
    var roundedTicks = (ticks / ticksPerMinute + 1) * ticksPerMinute;
    return Instant.FromUnixTimeTicks(roundedTicks);
}
