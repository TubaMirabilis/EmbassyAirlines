using Flights.Api.Domain.Flights;
using NodaTime;

namespace Flights.Api.Domain.Journeys;

public sealed class JourneyService : IJourneyService
{
    public IEnumerable<IEnumerable<Flight>> GetMultiLegJourneysOrderedByArrivalTime(IEnumerable<Flight> flights, string departure, string destination, LocalDate localDate, int count)
    {
        var flightsByDeparture = flights.GroupBy(f => f.DepartureAirport.IataCode)
                                        .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DepartureInstant).ToList());
        if (!flightsByDeparture.TryGetValue(departure, out var initialFlights))
        {
            return Array.Empty<Flight[]>();
        }
        var allFlights = initialFlights.Where(f => f.DepartureLocalTime.Date == localDate)
.ToList();
        var journeys = new List<Flight[]>();
        foreach (var flight in allFlights)
        {
            if (flightsByDeparture.TryGetValue(flight.ArrivalAirport.IataCode, out var nextFlightCandidates))
            {
                var nextFlights = nextFlightCandidates.Where(f => f.DepartureInstant > flight.ArrivalInstant.Plus(Duration.FromMinutes(30)))
                                                      .ToList();
                foreach (var nextFlight in nextFlights)
                {
                    if (nextFlight.ArrivalAirport.IataCode == destination)
                    {
                        journeys.Add([flight, nextFlight]);
                    }
                    else
                    {
                        if (flightsByDeparture.TryGetValue(nextFlight.ArrivalAirport.IataCode, out var finalFlightCandidates))
                        {
                            var finalFlights = finalFlightCandidates.Where(f => f.DepartureInstant > nextFlight.ArrivalInstant.Plus(Duration.FromMinutes(30)))
                                                                    .ToList();
                            foreach (var finalFlight in finalFlights)
                            {
                                if (finalFlight.ArrivalAirport.IataCode == destination)
                                {
                                    journeys.Add([flight, nextFlight, finalFlight]);
                                }
                            }
                        }
                    }
                }
            }
        }
        return journeys.OrderBy(j => j[^1].ArrivalLocalTime)
                       .Take(count)
                       .ToList();
    }
}
