using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Domain;

namespace EmbassyAirlines.Application.Repositories;

public interface IFleetRepository
{
    Task<IEnumerable<Aircraft>> GetFleetAsync(CancellationToken cancellationToken);
    Task<Aircraft?> GetAircraftByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAircraftAsync(Aircraft aircraft, CancellationToken cancellationToken);
    Task<int> UpdateAircraftAsync(Guid id, UpdateAircraftDto updatedAircraft, CancellationToken cancellationToken);
}