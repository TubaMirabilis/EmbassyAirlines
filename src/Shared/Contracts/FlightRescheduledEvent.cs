namespace Shared.Contracts;

public sealed record FlightRescheduledEvent(Guid FlightId, DateTime DepartureLocalTime, DateTime ArrivalLocalTime);
