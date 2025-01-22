using Flights.Api.Domain.Seats;
using NodaTime;

namespace Flights.Api.Domain.Bookings;

public sealed class Booking
{
    private Booking(Seat seat, string reference, string passengerName, string passengerEmail)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        Reference = reference;
        Seat = seat;
        PassengerName = passengerName;
        PassengerEmail = passengerEmail;
    }
#pragma warning disable CS8618
    private Booking()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; private set; }
    public Instant CreatedAt { get; private set; }
    public Instant UpdatedAt { get; private set; }
    public string Reference { get; private set; }
    public Seat Seat { get; private set; }
    public string PassengerName { get; private set; }
    public string? PassengerEmail { get; private set; }
    public static Booking Create(Seat seat, string reference, string passengerName, string passengerEmail)
        => new(seat, reference, passengerName, passengerEmail);
}
