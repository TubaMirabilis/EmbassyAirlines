namespace Shared.Contracts;

public sealed record CreateOrUpdateFlightDto(DateTime DepartureLocalTime, DateTime ArrivalLocalTime);
