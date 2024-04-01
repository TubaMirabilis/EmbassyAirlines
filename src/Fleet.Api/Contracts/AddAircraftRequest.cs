namespace Fleet.Api.Contracts;

public sealed record AddAircraftRequest(string Registration, string AircraftStatus, string OperationalStatus,
    string Location, string Model, string Type, string TypeDesignator,
    short Wingspan, string EngineModel, byte EngineCount, int ServiceCeiling,
    Dictionary<string, short> SeatingConfiguration, float FlightHours,
    DateTime ProductionDate, int BasicEmptyWeight, int MaximumZeroFuelWeight,
    int MaximumTakeoffWeight, int MaximumLandingWeight, int MaximumCargoWeight,
    int FuelOnboard, int FuelCapacity, byte MinimumCabinCrew);
