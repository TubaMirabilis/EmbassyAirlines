using Flights.Api.Domain.Flights;
using NodaTime;

namespace Flights.Api.Domain.Journeys;

public interface IJourneyService
{
    IEnumerable<IEnumerable<Flight>> GetMultiLegJourneysOrderedByArrivalTime(IEnumerable<Flight> flights, string departure, string destination, LocalDate localDate, int count);
}
