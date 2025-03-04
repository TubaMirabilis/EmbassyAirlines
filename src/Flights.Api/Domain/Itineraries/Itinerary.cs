using Flights.Api.Domain.Bookings;
using NodaTime;

namespace Flights.Api.Domain.Itineraries;

public sealed class Itinerary
{
    private readonly List<Booking> _bookings = [];
    private Itinerary(IEnumerable<Booking> bookings, string reference, string? leadPassengerEmail)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        Reference = reference;
        LeadPassengerEmail = leadPassengerEmail ?? "";
        _bookings.AddRange(bookings);
    }
#pragma warning disable CS8618
    private Itinerary()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; init; }
    public Instant UpdatedAt { get; private set; }
    public string Reference { get; init; }
    public string LeadPassengerEmail { get; private set; }
    public IReadOnlyList<Booking> Bookings => _bookings.AsReadOnly();
    public static Itinerary Create(IEnumerable<Booking> bookings, string reference, string? leadPassengerEmail) => new(bookings, reference, leadPassengerEmail);
}
