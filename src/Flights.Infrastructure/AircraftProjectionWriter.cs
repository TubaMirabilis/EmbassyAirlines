using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Flights.Infrastructure;

public sealed class AircraftProjectionWriter : IAircraftProjectionWriter
{
    private readonly ApplicationDbContext _ctx;
    public AircraftProjectionWriter(ApplicationDbContext ctx) => _ctx = ctx;
    public async Task InsertIfAbsentAsync(Guid id, string tailNumber, string equipmentCode, Instant createdAt, CancellationToken cancellationToken) => await _ctx.Database.ExecuteSqlInterpolatedAsync(
    $"""
            INSERT INTO flights.aircraft (
                id,
                tail_number,
                equipment_code,
                created_at
            )
            VALUES (
                {id},
                {tailNumber},
                {equipmentCode},
                {createdAt}
            )
            ON CONFLICT (id) DO NOTHING;
    """, cancellationToken);
}
