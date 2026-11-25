using NodaTime;

namespace Flights.Infrastructure;

public interface IAircraftProjectionWriter
{
    Task InsertIfAbsentAsync(AircraftSnapshot snapshot, CancellationToken cancellationToken);
}
