using Aircraft.Infrastructure.Database;
using ErrorOr;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Aircraft.Api.Lambda.Endpoints;

internal sealed class GetAircraftEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("aircraft/{id}", InvokeAsync)
              .WithSummary("Get an aircraft by ID")
              .Produces<AircraftDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<AircraftDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                                                       ILogger<GetAircraftEndpoint> logger,
                                                                                       Guid id,
                                                                                       CancellationToken ct)
    {
        var aircraft = await ctx.Aircraft
                                 .Include(a => a.Seats)
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aircraft is null)
        {
            logger.LogWarning("Aircraft with ID {Id} not found", id);
            var error = Error.NotFound("Aircraft.NotFound", $"Aircraft with ID {id} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        return TypedResults.Ok(aircraft.ToDto());
    }
}
