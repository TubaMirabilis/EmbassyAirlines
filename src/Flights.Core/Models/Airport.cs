using NodaTime;
using Shared;

namespace Flights.Core.Models;

public sealed class Airport
{
    private Airport(AirportCreationArgs args)
    {
        Ensure.NotEmpty(args.Id);
        Ensure.NotNullOrEmpty(args.IcaoCode);
        Ensure.NotNullOrEmpty(args.IataCode);
        Ensure.NotNullOrEmpty(args.Name);
        var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(args.TimeZoneId);
        ArgumentNullException.ThrowIfNull(timeZone);
        Id = args.Id;
        CreatedAt = args.CreatedAt;
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
    public Instant CreatedAt { get; init; }
    public Instant UpdatedAt { get; private set; }
    public string TimeZoneId { get; private set; }
    public DateTimeZone TimeZone => DateTimeZoneProviders.Tzdb[TimeZoneId];
    public string IataCode { get; private set; }
    public string IcaoCode { get; private set; }
    public string Name { get; private set; }
    public static Airport Create(AirportCreationArgs args) => new(args);
    public void Update(string icaoCode, string iataCode, string name, string timeZoneId, Instant instant)
    {
        Ensure.NotNullOrEmpty(icaoCode);
        Ensure.NotNullOrEmpty(iataCode);
        Ensure.NotNullOrEmpty(name);
        var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId);
        ArgumentNullException.ThrowIfNull(timeZone);
        IcaoCode = icaoCode;
        IataCode = iataCode;
        Name = name;
        TimeZoneId = timeZoneId;
        UpdatedAt = instant;
    }
}
