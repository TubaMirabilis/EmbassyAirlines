using NodaTime;

namespace Flights.Api;

public sealed class Airport
{
    private Airport(Guid id, string timeZoneId)
    {
        Id = id;
        TimeZoneId = timeZoneId;
    }
    #pragma warning disable CS8618
    private Airport()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public string TimeZoneId { get; private set; }
    public DateTimeZone TimeZone => DateTimeZoneProviders.Tzdb[TimeZoneId];
    public static Airport Create(Guid id, string timeZoneId) => new(id, timeZoneId);
}
