using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Domain;

namespace EmbassyAirlines.Application.Repositories;

public interface IFleetRepository
{
    Task<IEnumerable<Aircraft>> GetFleetAsync(CancellationToken ct);
    Task<Aircraft?> GetAircraftByIdAsync(Guid id, CancellationToken ct);
    Task AddAircraftAsync(Aircraft aircraft, CancellationToken ct);
    Task<int> UpdateAircraftAsync(Guid id, UpdateAircraftDto updatedAircraft, CancellationToken ct);
    Task<int> DeleteAircraftAsync(Guid id, CancellationToken ct);
}