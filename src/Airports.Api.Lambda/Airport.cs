using System.Text.Json.Serialization;

namespace Airports.Api.Lambda;

public sealed class Airport
{
    private Airport(string iataCode, string name, string timeZoneId)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Name = name;
        IataCode = iataCode;
        TimeZoneId = timeZoneId;
    }
#pragma warning disable CS8618
    [JsonConstructor]
    private Airport()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string Name { get; private set; }
    public string IataCode { get; private set; }
    public string TimeZoneId { get; private set; }
    public static Airport Create(string iataCode, string name, string timeZoneId) => new(iataCode, name, timeZoneId);
    public void Update(string iataCode, string name, string timeZoneId)
    {
        IataCode = iataCode;
        Name = name;
        TimeZoneId = timeZoneId;
        UpdatedAt = DateTime.UtcNow;
    }
}
