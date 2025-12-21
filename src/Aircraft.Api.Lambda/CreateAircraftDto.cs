namespace Aircraft.Api.Lambda;

internal sealed record CreateAircraftDto(string TailNumber, string EquipmentCode,
    int DryOperatingWeight, string Status, int MaximumTakeoffWeight, string? ParkedAt,
    string? EnRouteTo, int MaximumLandingWeight, int MaximumZeroFuelWeight, int MaximumFuelWeight);
