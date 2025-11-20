using Shared.Contracts;

namespace Aircraft.Api.Lambda;

internal static class AircraftExtensions
{
    public static AircraftDto ToDto(this Aircraft aircraft) => new(
        aircraft.Id,
        aircraft.TailNumber,
        aircraft.EquipmentCode,
        aircraft.DryOperatingWeight.Kilograms,
        aircraft.MaximumTakeoffWeight.Kilograms,
        aircraft.MaximumLandingWeight.Kilograms,
        aircraft.MaximumZeroFuelWeight.Kilograms,
        aircraft.MaximumFuelWeight.Kilograms,
        aircraft.Seats.Select(s => s.ToDto()).ToList()
    );
}
