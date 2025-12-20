using NodaTime;
using Shared;

namespace Flights.Core.Models;

public sealed class Aircraft
{
    private Aircraft(Guid id, string tailNumber, string equipmentCode, Instant instant)
    {
        Ensure.NotEmpty(id);
        Ensure.NotNullOrEmpty(tailNumber);
        Ensure.NotNullOrEmpty(equipmentCode);
        Id = id;
        CreatedAt = instant;
        TailNumber = tailNumber;
        EquipmentCode = equipmentCode;
    }
#pragma warning disable CS8618
    private Aircraft()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; init; }
    public string TailNumber { get; private set; }
    public string EquipmentCode { get; private set; }
    public static Aircraft Create(Guid id, string tailNumber, string equipmentCode, Instant instant) => new(id, tailNumber, equipmentCode, instant);
}
