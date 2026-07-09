namespace Shared.Abstractions;

public interface IFlightStatusManagementEvent : IDomainEvent
{
    Guid FlightId { get; }
}
