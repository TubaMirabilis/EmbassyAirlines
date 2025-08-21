namespace Shared.Contracts;

public sealed record AircraftCreatedEvent(Guid Id, string TailNumber, string EquipmentCode);
