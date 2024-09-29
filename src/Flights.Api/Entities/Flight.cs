namespace Flights.Api.Entities;

public sealed class Flight
{
    public required Guid Id { get; set; }
    public required string Departure { get; set; }
    public required string Destination { get; set; }
    public required DateTime DepartureTime { get; set; }
    public required DateTime ArrivalTime { get; set; }
    public required decimal Price { get; set; }
    public required int AvailableSeats { get; set; }
}
