using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Flights.Infrastructure;

public sealed class AircraftProjectionWriter : IAircraftProjectionWriter
{
    private readonly ApplicationDbContext _ctx;
    public AircraftProjectionWriter(ApplicationDbContext ctx) => _ctx = ctx;
    public async Task InsertIfAbsentAsync(AircraftSnapshot snapshot, CancellationToken cancellationToken) => await _ctx.Database.ExecuteSqlInterpolatedAsync(
    $"""
            INSERT INTO flights.aircraft (
                id,
                tail_number,
                equipment_code,
                created_at
            )
            VALUES (
                {snapshot.Id},
                {snapshot.TailNumber},
                {snapshot.EquipmentCode},
                {snapshot.CreatedAt}
            )
            ON CONFLICT (id) DO NOTHING;
    """, cancellationToken);
}
