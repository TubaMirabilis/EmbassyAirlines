using NodaTime;

namespace Flights.Api.Domain.Passengers;

public sealed class Passenger
{
    private Passenger(string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        FirstName = firstName;
        LastName = lastName;
    }
#pragma warning disable CS8618
    private Passenger()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; init; }
    public Instant UpdatedAt { get; private set; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public Guid BookingId { get; init; }
    public static Passenger Create(string firstName, string lastName) => new(firstName, lastName);
}
