using System.Linq.Expressions;
using Aircraft.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure;

internal sealed class MatchingCompletedFlightSpecification
{
    public MatchingCompletedFlightSpecification(Guid aircraftId, CompletedFlight flight) => Criteria = cf =>
        cf.Id == flight.Id
        && EF.Property<Guid>(cf, "AircraftId") == aircraftId
        && cf.DepartureAirportIcao == flight.DepartureAirportIcao
        && cf.ArrivalAirportIcao == flight.ArrivalAirportIcao
        && cf.Milestones == flight.Milestones;
    public Expression<Func<CompletedFlight, bool>> Criteria { get; }
}
