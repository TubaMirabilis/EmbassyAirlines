using NodaTime;

namespace Flights.Api;

internal sealed class Aircraft
{
    private Aircraft(Guid id, string tailNumber, string equipmentCode)
    {
        Id = id;
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
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
    public static Aircraft Create(Guid id, string tailNumber, string equipmentCode) => new(id, tailNumber, equipmentCode);
}
