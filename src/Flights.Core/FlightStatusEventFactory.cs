using Flights.Core.Models;
using Shared.Abstractions;
using Shared.Contracts;

namespace Flights.Core;

internal static class FlightStatusEventFactory
{
    public static IFlightStatusManagementEvent Create(Flight flight, FlightStatus status) => status switch
    {
        FlightStatus.Arrived => HandleArrival(flight),
        FlightStatus.Cancelled => new FlightCancelledEvent(Guid.NewGuid(), flight.Id),
        FlightStatus.Delayed => new FlightDelayedEvent(Guid.NewGuid(), flight.Id),
        FlightStatus.EnRoute => HandleDeparture(flight),
        _ => throw new InvalidOperationException("No event defined for the given flight status.")
    };
    private static IFlightStatusManagementEvent HandleDeparture(Flight flight)
    {
        if (flight.OffBlockAt is not { } offBlockAt)
        {
            throw new InvalidOperationException("Cannot transition to EnRoute without an OffBlock milestone.");
        }
        return flight.OffBlockAt <= flight.DepartureInstant ? new FlightMarkedAsEnRouteEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.ArrivalAirport.IcaoCode, offBlockAt.ToDateTimeOffset()) : new FlightMarkedAsDelayedEnRouteEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.ArrivalAirport.IcaoCode, offBlockAt.ToDateTimeOffset());
    }
    private static FlightArrivedEvent HandleArrival(Flight flight)
    {
        if (flight.OffBlockAt is not { } offBlockAt || flight.TakeoffAt is not { } takeoffAt || flight.LandingAt is not { } landingAt || flight.OnBlockAt is not { } onBlockAt)
        {
            throw new InvalidOperationException("Cannot transition to Arrived with incomplete milestone information.");
        }
        return new FlightArrivedEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.DepartureAirport.IcaoCode, flight.ArrivalAirport.IcaoCode, offBlockAt.ToDateTimeOffset(), takeoffAt.ToDateTimeOffset(), landingAt.ToDateTimeOffset(), onBlockAt.ToDateTimeOffset());
    }
}
