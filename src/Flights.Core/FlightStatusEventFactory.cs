using Flights.Core.Models;
using Shared.Abstractions;
using Shared.Contracts;

namespace Flights.Core;

internal static class FlightStatusEventFactory
{
    public static IFlightStatusManagementEvent Create(Flight flight, FlightStatus status) => status switch
    {
        FlightStatus.Cancelled => new FlightCancelledEvent(Guid.NewGuid(), flight.Id),
        FlightStatus.Arrived => new FlightArrivedEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.ArrivalAirport.IcaoCode),
        FlightStatus.Delayed => new FlightDelayedEvent(Guid.NewGuid(), flight.Id),
        FlightStatus.DelayedEnRoute => new FlightMarkedAsDelayedEnRouteEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.ArrivalAirport.IcaoCode),
        FlightStatus.EnRoute => new FlightMarkedAsEnRouteEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.ArrivalAirport.IcaoCode),
        _ => throw new InvalidOperationException("No event defined for the given flight status.")
    };
}
