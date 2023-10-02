using EmbassyAirlines.Application.Dtos;
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
    public async Task<IEnumerable<Aircraft>> GetFleetAsync(CancellationToken ct = default)
        => await _ctx.Aircraft.ToListAsync(ct);

    public async Task<Aircraft?> GetAircraftByIdAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Aircraft.FindAsync(id, ct);

    public async Task AddAircraftAsync(Aircraft aircraft, CancellationToken ct = default)
    {
        using var transaction = await _ctx.Database.BeginTransactionAsync(ct);
        try
        {
            _ctx.Aircraft.Add(aircraft);
            await _ctx.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
    public async Task<int> UpdateAircraftAsync(Guid id, UpdateAircraftDto updatedAircraft, CancellationToken ct = default)
        => await _ctx.Aircraft.Where(a => a.Id == id)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(a => a.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(a => a.Registration, updatedAircraft.Registration)
                    .SetProperty(a => a.Model, updatedAircraft.Model)
                    .SetProperty(a => a.Type, updatedAircraft.Type)
                    .SetProperty(a => a.EconomySeats, updatedAircraft.EconomySeats)
                    .SetProperty(a => a.BusinessSeats, updatedAircraft.BusinessSeats)
                    .SetProperty(a => a.FlightHours, updatedAircraft.FlightHours)
                    .SetProperty(a => a.BasicEmptyWeight, updatedAircraft.BasicEmptyWeight)
                    .SetProperty(a => a.MaximumZeroFuelWeight, updatedAircraft.MaximumZeroFuelWeight)
                    .SetProperty(a => a.MaximumTakeoffWeight, updatedAircraft.MaximumTakeoffWeight)
                    .SetProperty(a => a.MaximumLandingWeight, updatedAircraft.MaximumLandingWeight)
                    .SetProperty(a => a.MaximumCargoWeight, updatedAircraft.MaximumCargoWeight)
                    .SetProperty(a => a.FuelOnboard, updatedAircraft.FuelOnboard)
                    .SetProperty(a => a.FuelCapacity, updatedAircraft.FuelCapacity)
                    .SetProperty(a => a.MinimumCabinCrew, updatedAircraft.MinimumCabinCrew), ct);
    public async Task<int> DeleteAircraftAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Aircraft.Where(a => a.Id == id)
            .ExecuteDeleteAsync(ct);
}