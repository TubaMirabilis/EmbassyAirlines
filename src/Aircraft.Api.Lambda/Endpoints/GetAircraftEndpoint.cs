using Aircraft.Api.Lambda.Database;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Aircraft.Api.Lambda.Endpoints;

internal sealed class GetAircraftEndpoint : IEndpoint
{
    private readonly ILogger<GetAircraftEndpoint> _logger;
    public GetAircraftEndpoint(ILogger<GetAircraftEndpoint> logger) => _logger = logger;
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("aircraft/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(ApplicationDbContext ctx, Guid id, CancellationToken ct)
    {
        var aircraft = await ctx.Aircraft
                                 .Include(a => a.Seats)
                                 .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aircraft is null)
        {
            _logger.LogWarning("Aircraft with ID {Id} not found", id);
            var error = Error.NotFound("Aircraft.NotFound", $"Aircraft with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        return TypedResults.Ok(aircraft.ToDto());
    }
}
