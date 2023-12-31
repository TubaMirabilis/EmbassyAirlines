namespace EmbassyAirlines.Domain;

public class Aircraft
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string Registration { get; set; }
    public required string Model { get; set; }
    public required string Type { get; set; }
    public required int EconomySeats { get; set; }
    public required int BusinessSeats { get; set; }
    public required float FlightHours { get; set; }
    public required int BasicEmptyWeight { get; set; }
    public required int MaximumZeroFuelWeight { get; set; }
    public required int MaximumTakeoffWeight { get; set; }
    public required int MaximumLandingWeight { get; set; }
    public required int MaximumCargoWeight { get; set; }
    public required int FuelOnboard { get; set; }
    public required int FuelCapacity { get; set; }
    public required int MinimumCabinCrew { get; set; }
}