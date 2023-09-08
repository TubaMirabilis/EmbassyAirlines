using EmbassyAirlines.Domain;

namespace EmbassyAirlines.Application.Repositories;

public interface IFleetRepository
{
    Task<IEnumerable<Aircraft>> GetFleet(CancellationToken cancellationToken);
    Task<Aircraft?> GetAircraftById(Guid id, CancellationToken cancellationToken);
    Task AddAircraft(Aircraft aircraft, CancellationToken cancellationToken);
}