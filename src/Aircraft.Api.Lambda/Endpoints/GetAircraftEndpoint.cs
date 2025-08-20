using Aircraft.Api.Lambda.Database;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Aircraft.Api.Lambda.Endpoints;

internal sealed class GetAircraftEndpoint : IEndpoint
{
    private readonly ApplicationDbContext _ctx;
    public GetAircraftEndpoint(ApplicationDbContext ctx) => _ctx = ctx;
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("aircraft/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CancellationToken ct)
    {
        var aircraft = await _ctx.Aircraft
                                 .Include(a => a.Seats)
                                 .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aircraft is null)
        {
            var error = Error.NotFound("Aircraft", $"Aircraft with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        return TypedResults.Ok(aircraft.ToDto());
    }
}
