namespace Flights.Api.Entities;

public sealed class Seat
{
    private Seat(string seatNumber, SeatType seatType, decimal price)
    {
        Id = Guid.NewGuid();
        SeatNumber = seatNumber;
        SeatType = seatType;
        IsAvailable = true;
        Price = price;
    }
#pragma warning disable CS8618
    private Seat()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; private set; }
    public string SeatNumber { get; private set; }
    public SeatType SeatType { get; private set; }
    public bool IsAvailable { get; private set; }
    public decimal Price { get; private set; }
    public Guid FlightId { get; set; }
    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }
    public static Seat Create(string seatNumber, SeatType seatType, decimal price)
        => new(seatNumber, seatType, price);
}
public enum SeatType
{
    Economy,
    Business
}
