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
        private readonly ILogger<Handler> _logger;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }
        public async Task<ErrorOr<AircraftResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting aircraft by id {id}", request.Id);
            var aircraftResponse = await _ctx.Aircraft
                                         .AsNoTracking()
                                         .Where(aircraft => aircraft.Id == request.Id)
                                         .Select(aircraft => new AircraftMapper()
                                             .MapAircraftToAircraftResponse(aircraft))
                                         .FirstOrDefaultAsync(cancellationToken);
            if (aircraftResponse is null)
            {
                _logger.LogWarning("Aircraft not found");
                return Error.NotFound("Aircraft not found");
            }
            _logger.LogInformation("Aircraft found");
            return aircraftResponse;
        }
    }
}
public class GetAircraftEndpoint : ICarterModule
{
    private readonly ILogger<GetAircraftEndpoint> _logger;
    public GetAircraftEndpoint(ILogger<GetAircraftEndpoint> logger)
    {
        _logger = logger;
    }
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/aircraft/{id}", async (Guid id, ISender sender) =>
        {
            _logger.LogInformation("Getting aircraft by id {id}", id);
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
