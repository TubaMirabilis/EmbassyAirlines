namespace Flights.Infrastructure;

public interface IAirportProjectionWriter
{
    Task ApplyAsync(
        AirportSnapshot snapshot,
        CancellationToken cancellationToken);
}
