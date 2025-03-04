using Flights.Api.Domain.Itineraries;
using Shared.Contracts;

namespace Flights.Api.Extensions;

internal static class ItineraryExtensions
{
    public static ItineraryDto ToDto(this Itinerary itinerary) => new(
     itinerary.Bookings.Select(booking => booking.ToDto()),
        itinerary.Reference,
        itinerary.LeadPassengerEmail,
        itinerary.Bookings.Sum(booking => booking.TotalPrice));
}
