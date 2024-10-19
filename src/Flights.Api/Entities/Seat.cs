namespace Flights.Api.Entities;

public sealed class Seat
{
    private Seat(string seatNumber, SeatType seatType)
    {
        Id = Guid.NewGuid();
        SeatNumber = seatNumber;
        SeatType = seatType;
        IsAvailable = true;
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
    // Public method to alter the IsAvailable property:
    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }
    public static Seat Create(string seatNumber, SeatType seatType)
        => new(seatNumber, seatType);
}

public enum SeatType
{
    Economy,
    Business
}
