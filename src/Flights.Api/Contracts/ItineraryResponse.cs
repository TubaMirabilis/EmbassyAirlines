namespace Flights.Api.Contracts;

public sealed record ItineraryResponse
{
    public string DepartureAirportIata => Flights.First().DepartureAirport.Iata;
    public string ArrivalAirportIata => Flights.Last().ArrivalAirport.Iata;
    public int Stops => Flights.Count() - 1;
    public required IEnumerable<FlightResponse> Flights { get; init; }
}
