using Fleet.Api.Enums;

namespace Fleet.Api.Entities;

public sealed class Aircraft
{
    public required Guid Id { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required string Registration { get; set; }
    public required AircraftStatus Status { get; set; }
    public required string Location { get; set; }
    public required string Model { get; set; }
    public required AircraftType Type { get; set; }
    public required string TypeDesignator { get; set; }
    public required short Wingspan { get; set; }
    public required string EngineModel { get; set; }
    public required byte EngineCount { get; set; }
    public required int ServiceCeiling { get; set; }
    public required Dictionary<string, short> SeatingConfiguration { get; set; }
    public required float FlightHours { get; set; }
    public required DateTime ProductionDate { get; set; }
    public required int BasicEmptyWeight { get; set; }
    public required int MaximumZeroFuelWeight { get; set; }
    public required int MaximumTakeoffWeight { get; set; }
    public required int MaximumLandingWeight { get; set; }
    public required int MaximumCargoWeight { get; set; }
    public required int FuelOnboard { get; set; }
    public required int FuelCapacity { get; set; }
    public required byte MinimumCabinCrew { get; set; }
}
