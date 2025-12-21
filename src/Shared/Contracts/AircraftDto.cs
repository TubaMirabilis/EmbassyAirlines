namespace Shared.Contracts;

public sealed record AircraftDto(Guid Id, string TailNumber, string EquipmentCode,
    int DryOperatingWeight, int MaximumTakeoffWeight, int MaximumLandingWeight,
    int MaximumZeroFuelWeight, int MaximumFuelWeight, int Seats);
