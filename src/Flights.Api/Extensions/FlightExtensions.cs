using Flights.Api.Domain.Flights;
using Flights.Api.Domain.Seats;
using Shared.Contracts;

namespace Flights.Api.Extensions;

public static class FlightExtensions
{
    public static FlightDto ToDto(this Flight flight)
        => new FlightDto(
            flight.Id,
            flight.CreatedAt.ToDateTimeOffset(),
            flight.UpdatedAt.ToDateTimeOffset(),
            flight.FlightNumber,
            flight.Schedule.DepartureAirport.IataCode,
            flight.Schedule.DestinationAirport.IataCode,
            flight.Schedule.DepartureTime.ToDateTimeOffset(),
            flight.Schedule.ArrivalTime.ToDateTimeOffset(),
            flight.CheapestEconomyPrice,
            flight.CheapestBusinessPrice,
            flight.Seats.Count(s => s.IsAvailable && s.SeatType == SeatType.Economy),
            flight.Seats.Count(s => s.IsAvailable && s.SeatType == SeatType.Business)
        );
}
