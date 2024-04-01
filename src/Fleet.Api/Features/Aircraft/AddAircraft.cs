using Carter;
using ErrorOr;
using Fleet.Api.Contracts;
using Fleet.Api.Database;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Fleet.Api.Features.Aircraft;

public static class AddAircraft
{
    public sealed record Command(AddAircraftRequest Request) : IRequest<ErrorOr<AircraftResponse>>;
    internal sealed class Handler : IRequestHandler<Command, ErrorOr<AircraftResponse>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }
        public async Task<ErrorOr<AircraftResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding aircraft");
            var mapper = new AircraftMapper();
            var aircraft = mapper.MapAddAircraftRequestToAircraft(request.Request);
            _ctx.Aircraft.Add(aircraft);
            await _ctx.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Aircraft added");
            return mapper.MapAircraftToAircraftResponse(aircraft);
        }
    }
}
public class AddAircraftEndpoint : ICarterModule
{
    private readonly ILogger<AddAircraftEndpoint> _logger;
    public AddAircraftEndpoint(ILogger<AddAircraftEndpoint> logger)
    {
        _logger = logger;
    }
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/aircraft", async (AddAircraftRequest request,
            ISender sender, IOutputCacheStore cache, CancellationToken ct) =>
        {
            _logger.LogInformation("Adding aircraft");
            var command = new AddAircraft.Command(request);
            var result = await sender.Send(command, ct);
            if (!result.IsError)
            {
                await cache.EvictByTagAsync("aircraft", ct);
            }
            return result.Match(
                ac => Results.Created("/api/aircraft", ac),
                errors => ErrorHandlingHelper.HandleProblems(errors));
        }).WithName("Add aircraft")
        .WithOpenApi();
    }
}