using NodaTime;

namespace Flights.Api.Lambda;

public sealed class Airport
{
    private Airport(Guid id, string iataCode, string name, string timeZoneId)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
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
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }
    public string Name { get; private set; }
    public string IataCode { get; private set; }
    public string TimeZoneId { get; private set; }
    public DateTimeZone TimeZone => DateTimeZoneProviders.Tzdb[TimeZoneId];
    public static Airport Create(Guid id, string iataCode, string name, string timeZoneId) => new(id, iataCode, name, timeZoneId);
    public void Update(string iataCode, string name, string timeZoneId)
    {
        IataCode = iataCode;
        Name = name;
        TimeZoneId = timeZoneId;
        UpdatedAt = DateTime.UtcNow;
    }
}
