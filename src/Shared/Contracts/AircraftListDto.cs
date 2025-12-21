namespace Shared.Contracts;

public sealed record AircraftListDto(IEnumerable<AircraftDto> Items, int Page, int PageSize, int TotalItems, bool HasNextPage);
