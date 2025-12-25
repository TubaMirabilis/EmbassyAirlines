namespace Shared.Contracts;

public sealed record AircraftCreatedEvent(Guid Id, Guid AircraftId, string TailNumber, string EquipmentCode);
