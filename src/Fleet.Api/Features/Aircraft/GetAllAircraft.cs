using Carter;
using ErrorOr;
using Fleet.Api.Contracts;
using Fleet.Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Fleet.Api.Features.Aircraft;

public static class GetAllAircraft
{
    public sealed record Query : IRequest<ErrorOr<AircraftResponse[]>>;
    internal sealed class Handler : IRequestHandler<Query, ErrorOr<AircraftResponse[]>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }
        public async Task<ErrorOr<AircraftResponse[]>> Handle(Query request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting all aircraft");
            var mapper = new AircraftMapper();
            var aircraftResponses = await _ctx.Aircraft
                .AsNoTracking()
                .Select(aircraft => mapper.MapAircraftToAircraftResponse(aircraft))
                .ToArrayAsync(cancellationToken);
            _logger.LogInformation("Aircraft found");
            return aircraftResponses;
        }
    }
}
public class GetAllAircraftEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/aircraft", async (ISender sender, CancellationToken ct) =>
        {
            var query = new GetAllAircraft.Query();
            var result = await sender.Send(query, ct);
            return result.Match(
                ac => Results.Ok(result),
                errors => ErrorHandlingHelper.HandleProblems(errors));
        }).CacheOutput(x => x.Expire(TimeSpan.FromMinutes(5)).Tag("aircraft"))
            .WithName("Get all aircraft")
            .WithOpenApi();
    }
}
