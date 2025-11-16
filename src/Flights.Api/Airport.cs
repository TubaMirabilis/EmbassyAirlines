using NodaTime;
using Shared;

namespace Flights.Api;

internal sealed class Airport
{
    private Airport(AirportCreationArgs args)
    {
        Ensure.NotNullOrEmpty(args.IcaoCode);
        Ensure.NotNullOrEmpty(args.IataCode);
        Ensure.NotNullOrEmpty(args.Name);
        Ensure.NotNullOrEmpty(args.TimeZoneId);
        Id = args.Id;
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = CreatedAt;
        TimeZoneId = args.TimeZoneId;
        IataCode = args.IataCode;
        IcaoCode = args.IcaoCode;
        Name = args.Name;
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
    public static Airport Create(AirportCreationArgs args) => new(args);
    public void Update(string icaoCode, string iataCode, string name, string timeZoneId)
    {
        IcaoCode = icaoCode;
        IataCode = iataCode;
        Name = name;
        TimeZoneId = timeZoneId;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }
}
