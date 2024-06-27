namespace Flights.Api.Contracts;

public sealed record AircraftResponse(Guid Id, string TypeDesignator, string Registration);
