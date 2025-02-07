using Flights.Api.Domain.Flights;
using NodaTime;

namespace Flights.Api.Domain.Journeys;

public interface IJourneyService
{
    IEnumerable<IEnumerable<Flight>> GetThreeFastestMultiLegJourneys(IEnumerable<Flight> flights, string departure, string destination, LocalDate localDate);
}
