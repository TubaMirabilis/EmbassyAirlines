namespace Shared.Abstractions;

public interface IFlightStatusManagementEvent
{
    Guid FlightId { get; }
}
