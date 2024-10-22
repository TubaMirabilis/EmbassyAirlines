using NodaTime;

namespace Flights.Api.Domain.Seats;

public sealed class Seat
{
    private Seat(string seatNumber, SeatType seatType, decimal price)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
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
    public Instant CreatedAt { get; private set; }
    public Instant UpdatedAt { get; private set; }
    public string SeatNumber { get; private set; }
    public SeatType SeatType { get; private set; }
    public bool IsAvailable { get; private set; }
    public decimal Price { get; private set; }
    public Guid FlightId { get; set; }
    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }
    public static Seat Create(string seatNumber, SeatType seatType, decimal price)
        => new(seatNumber, seatType, price);
}
