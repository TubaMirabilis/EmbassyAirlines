using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class ListFlightsEndpoint : IEndpoint
{
        private readonly IServiceScopeFactory _factory;
    public ListFlightsEndpoint(IServiceScopeFactory factory) => _factory = factory;
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights", InvokeAsync);
    private async Task<IResult> InvokeAsync(CancellationToken ct)
    {
        using var scope = _factory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var flights = await ctx.Flights.ToListAsync(ct);
        var list = flights.Select(f => f.ToDto()).ToList();
        return TypedResults.Ok(list);
    }
}
