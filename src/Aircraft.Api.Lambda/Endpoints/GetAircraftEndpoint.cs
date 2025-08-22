using Aircraft.Api.Lambda.Database;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Aircraft.Api.Lambda.Endpoints;

internal sealed class GetAircraftEndpoint : IEndpoint
{
    private readonly IServiceScopeFactory _factory;
    public GetAircraftEndpoint(IServiceScopeFactory factory) => _factory = factory;
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("aircraft/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CancellationToken ct)
    {
        using var scope = _factory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aircraft = await ctx.Aircraft
                                 .Include(a => a.Seats)
                                 .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aircraft is null)
        {
            var error = Error.NotFound("Aircraft.NotFound", $"Aircraft with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        return TypedResults.Ok(aircraft.ToDto());
    }
}
