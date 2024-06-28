namespace Flights.Api.Domain.Flights;

public sealed record FlightCreationArgs
{
    public required string Number { get; set; }
    public required string NumberIataFormat { get; set; }
    public required string NumberIcaoFormat { get; set; }
    public required DateTime DepartureTimeUtc { get; set; }
    public required DateTime ArrivalTimeUtc { get; set; }
    public required Aircraft Aircraft { get; set; }
    public required FlightStatus Status { get; set; }
    public required string DepartureGate { get; set; }
    public required string ArrivalGate { get; set; }
    public required string DepartureTerminal { get; set; }
    public required string ArrivalTerminal { get; set; }
    public required Airport DepartureAirport { get; set; }
    public required Airport ArrivalAirport { get; set; }
    public required short AdultMen { get; set; }
    public required short AdultWomen { get; set; }
    public required short Children { get; set; }
    public required short CheckedBags { get; set; }
    public required string Notes { get; set; }
}
