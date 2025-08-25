﻿using System.Text.Json.Serialization;
using Shared;

namespace Airports.Api.Lambda;

internal sealed class Airport
{
    private Airport(string icaoCode, string iataCode, string name, string timeZoneId)
    {
        Ensure.NotNullOrEmpty(icaoCode);
        Ensure.NotNullOrEmpty(iataCode);
        Ensure.NotNullOrEmpty(name);
        Ensure.NotNullOrEmpty(timeZoneId);
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Name = name;
        IcaoCode = iataCode;
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
    public string IcaoCode { get; private set; }
    [JsonInclude]
    public string IataCode { get; private set; }
    [JsonInclude]
    public string TimeZoneId { get; private set; }
    public static Airport Create(string icaoCode, string iataCode, string name, string timeZoneId) => new(icaoCode, iataCode, name, timeZoneId);
    public void Update(string icaoCode, string iataCode, string name, string timeZoneId)
    {
        IcaoCode = icaoCode;
        IataCode = iataCode;
        Name = name;
        TimeZoneId = timeZoneId;
        UpdatedAt = DateTime.UtcNow;
    }
}
