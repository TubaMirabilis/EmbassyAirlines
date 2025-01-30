using Flights.Api.Domain.Flights;
using NodaTime;

namespace Flights.Api.Domain.Seats;

public sealed class Seat
{
    private Seat(SeatType seatType, string seatNumber, decimal price)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        SeatNumber = seatNumber;
        SeatType = seatType;
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
    public decimal Price { get; private set; }
    public Flight Flight { get; init; } = null!;
    public Guid FlightId { get; init; }
    public bool IsBooked => BookingId.HasValue;
    public Guid? BookingId { get; private set; }
    public void Book(Guid bookingId) => BookingId = bookingId;
    public static Seat Create(SeatType seatType, string seatNumber, decimal price)
        => new(seatType, seatNumber, price);
}
