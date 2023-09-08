namespace EmbassyAirlines.Application.Dtos;

public sealed record AircraftDto(Guid Id, DateTime CreatedAt,
    string Registration, string Model, string Type, int EconomySeats,
    int BusinessSeats, float FlightHours, int BasicEmptyWeight,
    int MaximumZeroFuelWeight, int MaximumTakeoffWeight,
    int MaximumLandingWeight, int MaximumCargoWeight, int FuelOnboard,
    int FuelCapacity, int MinimumCabinCrew);