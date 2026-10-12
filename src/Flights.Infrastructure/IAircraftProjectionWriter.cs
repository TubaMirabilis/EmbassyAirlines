using NodaTime;

namespace Flights.Infrastructure;

public interface IAircraftProjectionWriter
{
    Task InsertIfAbsentAsync(
        Guid id,
        string tailNumber,
        string equipmentCode,
        Instant createdAt,
        CancellationToken cancellationToken);
}
