using NodaTime;

namespace Flights.Api.Lambda.FunctionalTests;

public static class TimeHelpers
{
    public static Instant AdvanceToNextMinuteBoundary(Instant instant)
    {
        var ticks = instant.ToUnixTimeTicks();
        var ticksPerMinute = NodaConstants.TicksPerMinute;
        var roundedTicks = (ticks / ticksPerMinute + 1) * ticksPerMinute;
        return Instant.FromUnixTimeTicks(roundedTicks);
    }
    public static Instant MinutesFromNowRoundedUp(IClock clock, int minutes)
    {
        var target = clock.GetCurrentInstant() + Duration.FromMinutes(minutes);
        return AdvanceToNextMinuteBoundary(target);
    }
}
