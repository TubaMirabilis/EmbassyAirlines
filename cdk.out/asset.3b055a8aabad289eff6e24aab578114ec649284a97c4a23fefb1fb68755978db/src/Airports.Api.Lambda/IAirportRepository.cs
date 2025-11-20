using ErrorOr;

namespace Airports.Api.Lambda;

internal interface IAirportRepository
{
    Task<ErrorOr<Airport>> GetAirportByIdAsync(Guid id, CancellationToken ct);
    Task<bool> UpdateAirportAsync(Airport airport, CancellationToken ct);
}
