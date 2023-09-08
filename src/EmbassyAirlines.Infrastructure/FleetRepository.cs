using EmbassyAirlines.Application.Repositories;
using EmbassyAirlines.Domain;
using Microsoft.EntityFrameworkCore;

namespace EmbassyAirlines.Infrastructure;

internal sealed class FleetRepository : IFleetRepository
{
    private ApplicationDbContext _ctx;
    public FleetRepository(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }
    public async Task<IEnumerable<Aircraft>> GetFleet(CancellationToken cancellationToken = default)
        => await _ctx.Aircraft.ToListAsync();

    public async Task<Aircraft?> GetAircraftById(Guid id, CancellationToken cancellationToken = default)
        => await _ctx.Aircraft.FindAsync(id);
    public async Task AddAircraft(Aircraft aircraft, CancellationToken cancellationToken = default)
    {
        using var transaction = await _ctx.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _ctx.Aircraft.Add(aircraft);
            await _ctx.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}