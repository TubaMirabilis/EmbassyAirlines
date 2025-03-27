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
    [JsonInclude]
    public Guid Id { get; init; }
    [JsonInclude]
    public DateTime CreatedAt { get; init; }
    [JsonInclude]
    public DateTime UpdatedAt { get; private set; }
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public string IataCode { get; private set; }
    [JsonInclude]
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
