namespace Shared.Contracts;

public sealed record FlightListDto(IEnumerable<FlightDto> Items, int Page, int PageSize, int TotalItems, bool HasNextPage);
