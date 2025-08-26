namespace Shared.Contracts;

public sealed record RescheduleFlightDto(DateTime DepartureLocalTime, DateTime ArrivalLocalTime);
