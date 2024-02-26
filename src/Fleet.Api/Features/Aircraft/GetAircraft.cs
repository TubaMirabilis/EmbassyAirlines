using Carter;
using ErrorOr;
using Fleet.Api.Contracts;
using Fleet.Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Fleet.Api.Features.Aircraft;

public static class GetAircraft
{
    public class Query : IRequest<ErrorOr<AircraftResponse>>
    {
        public Guid Id { get; set; }
    }
    internal sealed class Handler : IRequestHandler<Query, ErrorOr<AircraftResponse>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx)
        {
            _ctx = ctx;
        }
        public async Task<ErrorOr<AircraftResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var aircraftResponse = await _ctx.Aircraft
                                         .AsNoTracking()
                                         .Where(aircraft => aircraft.Id == request.Id)
                                         .Select(aircraft => new AircraftMapper()
                                             .MapAircraftToAircraftResponse(aircraft))
                                         .FirstOrDefaultAsync(cancellationToken);
            if (aircraftResponse is null)
            {
                return Error.NotFound("Aircraft not found");
            }
            return aircraftResponse;
        }
    }
}
public class GetAircraftEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/aircraft/{id}", async (Guid id, ISender sender) =>
        {
            var query = new GetAircraft.Query { Id = id };
            var result = await sender.Send(query);
            return result.Match(
                ac => Results.Ok(result),
                errors => ErrorHandlingHelper.HandleProblems(errors));
        }).CacheOutput(x => x.AddPolicy<ByIdCachePolicy>())
            .WithName("Get aircraft by id")
            .WithOpenApi();
    }
}
