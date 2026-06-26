using NodaTime;

namespace SmokeTests;

internal static class FlightTimeCalculator
{
    public static (ZonedDateTime Departure, ZonedDateTime Arrival) CalculateFlightTimes(FlightTimeCalculationRequest request)
    {
        var departureInstant = FlightTimeCalculator.NextMinuteBoundaryAfter(request.LeadTime);
        var arrivalInstant = departureInstant + request.FlightDuration;
        var departureTimeZone = DateTimeZoneProviders.Tzdb[request.DepartureTimeZoneId];
        var arrivalTimeZone = DateTimeZoneProviders.Tzdb[request.ArrivalTimeZoneId];
        var departureZoned = departureInstant.InZone(departureTimeZone);
        var arrivalZoned = arrivalInstant.InZone(arrivalTimeZone);
        return (departureZoned, arrivalZoned);
    }
    private static Instant NextMinuteBoundaryAfter(Duration duration)
    {
        var clock = SystemClock.Instance;
        var target = clock.GetCurrentInstant() + duration;
        return AdvanceToNextMinuteBoundary(target);
    }
    private static Instant AdvanceToNextMinuteBoundary(Instant instant)
    {
        var ticks = instant.ToUnixTimeTicks();
        var ticksPerMinute = NodaConstants.TicksPerMinute;
        var roundedTicks = (ticks / ticksPerMinute + 1) * ticksPerMinute;
        return Instant.FromUnixTimeTicks(roundedTicks);
    }
}
