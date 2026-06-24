using NodaTime;

namespace SmokeTests;

internal static class FlightTimeCalculator
{
    public static (ZonedDateTime Departure, ZonedDateTime Arrival) CalculateFlightTimes(Duration leadTime, Duration flightDuration, string departureTimeZoneId, string arrivalTimeZoneId)
    {
        var departureInstant = FlightTimeCalculator.NextMinuteBoundaryAfter(leadTime);
        var arrivalInstant = departureInstant + flightDuration;
        var departureTimeZone = DateTimeZoneProviders.Tzdb[departureTimeZoneId];
        var arrivalTimeZone = DateTimeZoneProviders.Tzdb[arrivalTimeZoneId];
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

        var roundedTicks =
            (ticks / ticksPerMinute + 1) * ticksPerMinute;

        return Instant.FromUnixTimeTicks(roundedTicks);
    }
}
