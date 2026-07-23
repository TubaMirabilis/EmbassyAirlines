using Shared;
using Shared.Contracts;

namespace Airports.Core.Models;

public sealed class Airport : Entity
{
    private Airport(string icaoCode, string iataCode, string name, string timeZoneId, DateTimeOffset createdAt)
    {
        Ensure.NotNullOrEmpty(icaoCode);
        Ensure.NotNullOrEmpty(iataCode);
        Ensure.NotNullOrEmpty(name);
        Ensure.NotNullOrEmpty(timeZoneId);
        Id = Guid.NewGuid();
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Name = name;
        IcaoCode = icaoCode;
        IataCode = iataCode;
        TimeZoneId = timeZoneId;
        AddDomainEvent(new AirportCreatedEvent(Guid.NewGuid(), Id, Name, IcaoCode, IataCode, TimeZoneId));
    }
#pragma warning disable CS8618
    private Airport()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string Name { get; private set; }
    public string IcaoCode { get; private set; }
    public string IataCode { get; private set; }
    public string TimeZoneId { get; private set; }
    public static Airport Create(string icaoCode, string iataCode, string name, string timeZoneId, DateTimeOffset createdAt)
        => new(icaoCode, iataCode, name, timeZoneId, createdAt);
    public void Update(string icaoCode, string iataCode, string name, string timeZoneId, DateTimeOffset updatedAt)
    {
        IcaoCode = icaoCode;
        IataCode = iataCode;
        Name = name;
        TimeZoneId = timeZoneId;
        UpdatedAt = updatedAt;
        AddDomainEvent(new AirportUpdatedEvent(Guid.NewGuid(), Id, Name, IcaoCode, IataCode, TimeZoneId));
    }
}
