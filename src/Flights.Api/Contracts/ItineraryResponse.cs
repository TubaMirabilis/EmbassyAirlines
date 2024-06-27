namespace Flights.Api.Contracts;

public sealed record ItineraryResponse
{
    public string DepartureAirportIata => Flights.First().DepartureAirportIata;
    public string ArrivalAirportIata => Flights.Last().ArrivalAirportIata;
    public int Stops => Flights.Count() - 1;
    public required IEnumerable<FlightResponse> Flights { get; init; }
}
