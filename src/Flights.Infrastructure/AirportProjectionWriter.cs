using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Flights.Infrastructure;

public sealed class AirportProjectionWriter : IAirportProjectionWriter
{
    private readonly ApplicationDbContext _ctx;
    public AirportProjectionWriter(ApplicationDbContext ctx) => _ctx = ctx;
    public async Task ApplyAsync(AirportSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            snapshot.Version);
        await _ctx.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO flights.airports (
                id,
                version,
                name,
                icao_code,
                iata_code,
                time_zone_id,
                created_at,
                updated_at
            )
            VALUES (
                {snapshot.Id},
                {snapshot.Version},
                {snapshot.Name},
                {snapshot.IcaoCode},
                {snapshot.IataCode},
                {snapshot.TimeZoneId},
                {snapshot.CreatedAt},
                {snapshot.UpdatedAt}
            )
            ON CONFLICT (id)
            DO UPDATE SET
                version = EXCLUDED.version,
                name = EXCLUDED.name,
                icao_code = EXCLUDED.icao_code,
                iata_code = EXCLUDED.iata_code,
                time_zone_id = EXCLUDED.time_zone_id,
                updated_at = EXCLUDED.updated_at
            WHERE airports.version < EXCLUDED.version;
            """,
            cancellationToken);
    }
}
