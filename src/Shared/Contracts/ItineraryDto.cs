namespace Shared.Contracts;

public sealed record ItineraryDto(IEnumerable<BookingDto> Bookings, string Reference, string? LeadPassengerEmail, decimal TotalPrice);
