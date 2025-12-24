using Aircraft.Infrastructure.Database;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Aircraft.Api.Lambda.Endpoints;

internal sealed class ListAircraftEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("aircraft", InvokeAsync);
    private static async Task<IResult> InvokeAsync(ApplicationDbContext ctx,
                                                   int page = 1,
                                                   int pageSize = 50,
                                                   string? parkedAt = null,
                                                   string? enRouteTo = null,
                                                   CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(parkedAt) && !string.IsNullOrWhiteSpace(enRouteTo))
        {
            var error = Error.Validation("Aircraft.InvalidQuery", "Cannot filter by both parkedAt and enRouteTo simultaneously.");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = ctx.Aircraft
                       .Include(a => a.Seats)
                       .AsNoTracking();
        if (!string.IsNullOrWhiteSpace(parkedAt))
        {
            var parkedAtNorm = parkedAt.Trim().ToUpperInvariant();
            query = query.Where(a => a.ParkedAt == parkedAtNorm);
        }
        if (!string.IsNullOrWhiteSpace(enRouteTo))
        {
            var enRouteToNorm = enRouteTo.Trim().ToUpperInvariant();
            query = query.Where(a => a.EnRouteTo == enRouteToNorm);
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
        var list = await query.OrderBy(a => a.Id)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync(ct);
        var res = new AircraftListDto(list.Select(a => a.ToDto()), page, pageSize, count, page < pages);
        return TypedResults.Ok(res);
    }
}
