using NodaTime;
using Shared.Contracts;
using SmokeTests;

var baseAddress = args.Length > 0 ? args[0] : "https://embassyairlines.com/api/";
using var client = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};
await ServiceReadiness.EnsureReadyAsync(client, "airports");
var req1 = new CreateOrUpdateAirportDto("RKSI", "ICN", "Incheon International Airport", "Asia/Seoul");
var req2 = new CreateOrUpdateAirportDto("EHAM", "AMS", "Schiphol Airport", "Europe/Amsterdam");
var airport1 = await AirportsSmokeTestActions.AttemptPostAsync(client, req1);
var airport2 = await AirportsSmokeTestActions.AttemptPostAsync(client, req2);
await ServiceReadiness.EnsureReadyAsync(client, "aircraft");
await AircraftSmokeTestActions.PrepareS3Async("Resources/Layouts/B78X.json", "seat-layouts/B78X.json");
var req3 = new CreateAircraftDto("PH-JRN", "B78X", 135500, "Parked", 254011, "RKSI", null, 201848, 192777, 101522);
var aircraft = await AircraftSmokeTestActions.AttemptPostAsync(client, req3);
await ServiceReadiness.EnsureReadyAsync(client, "flights");
var flightTimes = new FlightTimeCalculationRequest
{
    LeadTime = Duration.FromMinutes(30),
    FlightDuration = Duration.FromHours(10).Plus(Duration.FromMinutes(30)),
    DepartureTimeZoneId = airport1.TimeZoneId,
    ArrivalTimeZoneId = airport2.TimeZoneId
};
var (departureZoned, arrivalZoned) = FlightTimeCalculator.CalculateFlightTimes(flightTimes);
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
