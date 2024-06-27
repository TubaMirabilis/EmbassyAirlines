namespace Flights.Api.Entities;

public sealed record ItineraryResponse
{
    public string DepartureAirportIata => Flights.First().DepartureAirportIata;
    public string ArrivalAirportIata => Flights.Last().ArrivalAirportIata;
    public int Stops => Flights.Count - 1;
    public required List<Flight> Flights { get; init; }
}
