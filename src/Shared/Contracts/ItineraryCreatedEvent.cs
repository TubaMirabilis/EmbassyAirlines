namespace Shared.Contracts;

public sealed record ItineraryCreatedEvent(IEnumerable<BookingDto> Bookings, string Reference, string? LeadPassengerEmail, decimal TotalPrice);
