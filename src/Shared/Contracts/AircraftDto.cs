namespace Aircraft.Api.Lambda;

public sealed record AircraftDto(Guid Id, string TailNumber, string EquipmentCode, int DryOperatingWeight, int MaximumTakeoffWeight, int MaximumLandingWeight, int MaximumZeroFuelWeight, int MaximumFuelWeight, IEnumerable<SeatDto> Seats);
