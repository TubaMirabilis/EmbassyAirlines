namespace EmbassyAirlines.Application.Dtos;

public sealed record UpdateAircraftDto(string Registration, string Model,
    string Type, int EconomySeats, int BusinessSeats, int FlightHours,
    int BasicEmptyWeight, int MaximumZeroFuelWeight, int MaximumTakeoffWeight,
    int MaximumLandingWeight, int MaximumCargoWeight, int FuelOnboard,
    int FuelCapacity, int MinimumCabinCrew);