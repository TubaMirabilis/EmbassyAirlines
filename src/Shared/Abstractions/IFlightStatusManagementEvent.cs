namespace Shared.Abstractions;

public interface IFlightStatusManagementEvent
{
    Guid Id { get; }
    Guid FlightId { get; }
}
