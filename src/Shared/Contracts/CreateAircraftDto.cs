namespace Shared.Contracts;

public sealed record CreateAircraftDto(string TailNumber, string EquipmentCode,
    int DryOperatingWeight, int MaximumTakeoffWeight, int MaximumLandingWeight,
    int MaximumZeroFuelWeight, int MaximumFuelWeight, AircraftSituationDto Situation);
