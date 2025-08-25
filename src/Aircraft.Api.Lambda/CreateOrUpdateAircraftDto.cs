namespace Aircraft.Api.Lambda;

internal sealed record CreateOrUpdateAircraftDto(string TailNumber, string EquipmentCode, int DryOperatingWeight, int MaximumTakeoffWeight, int MaximumLandingWeight, int MaximumZeroFuelWeight, int MaximumFuelWeight);
