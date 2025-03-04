namespace Shared.Contracts;

public sealed record CreateItineraryDto(IEnumerable<CreateBookingDto> Bookings, string? LeadPassengerEmail);
