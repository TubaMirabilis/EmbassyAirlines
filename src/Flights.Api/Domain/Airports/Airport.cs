using NodaTime;

namespace Flights.Api.Domain.Airports;

public sealed class Airport
{
    private Airport(string iataCode, string name, string timeZoneId)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        Name = name;
        IataCode = iataCode;
        TimeZoneId = timeZoneId;
    }
#pragma warning disable CS8618
    private Airport()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; private set; }
    public Instant UpdatedAt { get; private set; }
    public string Name { get; private set; }
    public string IataCode { get; private set; }
    public string TimeZoneId { get; private set; }
    public DateTimeZone TimeZone => DateTimeZoneProviders.Tzdb[TimeZoneId];
    public static Airport Create(string iataCode, string name, string timeZoneId) => new(iataCode, name, timeZoneId);
}
