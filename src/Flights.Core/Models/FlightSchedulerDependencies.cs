namespace Flights.Core.Models;

public sealed record FlightSchedulerDependencies(Aircraft Aircraft, Airport ArrivalAirport, Airport DepartureAirport);
