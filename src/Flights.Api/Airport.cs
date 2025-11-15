using NodaTime;
using Shared;

namespace Flights.Api;

internal sealed class Airport
{
    private Airport(Guid id, string timeZoneId, string iataCode, string icaoCode, string name)
    {
        Ensure.NotNullOrEmpty(icaoCode);
        Ensure.NotNullOrEmpty(iataCode);
        Ensure.NotNullOrEmpty(name);
        Ensure.NotNullOrEmpty(timeZoneId);
        Id = id;
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = CreatedAt;
        TimeZoneId = timeZoneId;
        IataCode = iataCode;
        IcaoCode = icaoCode;
        Name = name;
    }
#pragma warning disable CS8618
    private Airport()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; private set; }
    public Instant UpdatedAt { get; private set; }
    public string TimeZoneId { get; private set; }
    public DateTimeZone TimeZone => DateTimeZoneProviders.Tzdb[TimeZoneId];
    public string IataCode { get; private set; }
    public string IcaoCode { get; private set; }
    public string Name { get; private set; }
    public static Airport Create(Guid id, string timeZoneId, string iataCode, string icaoCode, string name) => new(id, timeZoneId, iataCode, icaoCode, name);
    public void Update(string icaoCode, string iataCode, string name, string timeZoneId)
    {
        IcaoCode = icaoCode;
        IataCode = iataCode;
        Name = name;
        TimeZoneId = timeZoneId;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }
}
