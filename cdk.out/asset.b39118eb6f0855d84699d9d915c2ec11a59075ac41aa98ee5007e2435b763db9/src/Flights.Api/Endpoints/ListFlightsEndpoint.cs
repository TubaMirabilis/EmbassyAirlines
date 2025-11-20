using Flights.Api.Database;
using Flights.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class ListFlightsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights", InvokeAsync);
    private static async Task<IResult> InvokeAsync(ApplicationDbContext ctx,
                                                   int page = 1,
                                                   int pageSize = 50,
                                                   string? from = null,
                                                   string? to = null,
                                                   CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = ctx.Flights
                       .AsNoTracking();
        if (!string.IsNullOrWhiteSpace(from))
        {
            var fromNorm = from.Trim().ToUpperInvariant();
            query = query.Where(f => f.DepartureAirport.IataCode == fromNorm);
        }
        if (!string.IsNullOrWhiteSpace(to))
        {
            var toNorm = to.Trim().ToUpperInvariant();
            query = query.Where(f => f.ArrivalAirport.IataCode == toNorm);
        }
        var count = await query.CountAsync(ct);
        var pages = (int)Math.Ceiling(count / (double)pageSize);
        if (pages == 0)
        {
            pages = 1;
        }
        if (page > pages)
        {
            page = pages;
        }
        var list = await query.OrderBy(f => f.DepartureLocalTime)
                              .ThenBy(f => f.Id)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync(ct);
        var res = new FlightListDto(list.Select(f => f.ToDto()).ToList(), page, pageSize, count, page < pages);
        return TypedResults.Ok(res);
    }
}
