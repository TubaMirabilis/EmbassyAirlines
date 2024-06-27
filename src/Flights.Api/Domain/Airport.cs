namespace Flights.Api.Domain;

public sealed class Airport
{
    public required Guid Id { get; set; }
    public required string Iata { get; set; }
    public required string Icao { get; set; }
    public required string FullName { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string TimeZoneId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
