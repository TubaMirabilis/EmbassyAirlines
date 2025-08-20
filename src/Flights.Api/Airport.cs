using NodaTime;

namespace Flights.Api;

public sealed class Airport
{
    private Airport(Guid id, string timeZoneId, string iataCode)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        TimeZoneId = timeZoneId;
        IataCode = iataCode;
    }
    #pragma warning disable CS8618
    private Airport()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string TimeZoneId { get; private set; }
    public DateTimeZone TimeZone => DateTimeZoneProviders.Tzdb[TimeZoneId];
    public string IataCode { get; private set; }
    public static Airport Create(Guid id, string timeZoneId, string iataCode) => new(id, timeZoneId, iataCode);
}
