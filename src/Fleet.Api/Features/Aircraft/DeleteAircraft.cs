﻿using Carter;
using ErrorOr;
using Fleet.Api.Database;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Fleet.Api.Features.Aircraft;

public static class DeleteAircraft
{
    public sealed record Command(Guid Id) : IRequest<ErrorOr<Unit>>;
    internal sealed class Handler : IRequestHandler<Command, ErrorOr<Unit>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }
        public async Task<ErrorOr<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting aircraft by id {id}", request.Id);
            var aircraft = await _ctx.Aircraft
                .Where(aircraft => aircraft.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (aircraft is null)
            {
                _logger.LogWarning("Aircraft not found");
                return Error.NotFound("Aircraft not found");
            }
            _ctx.Aircraft.Remove(aircraft);
            if (await _ctx.SaveChangesAsync(cancellationToken) == 0)
            {
                _logger.LogError("Failed to delete aircraft");
                return Error.Failure("Failed to delete aircraft");
            }
            _logger.LogInformation("Aircraft deleted");
            return Unit.Value;
        }
    }
}
public class DeleteAircraftEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/aircraft/{id}", async (Guid id, ISender sender,
            IOutputCacheStore cache, CancellationToken ct) =>
        {
            var command = new DeleteAircraft.Command(id);
            var result = await sender.Send(command, ct);
            if (!result.IsError)
            {
                await cache.EvictByTagAsync("aircraft", ct);
            }
            return result.Match(
                _ => Results.NoContent(),
                errors => ErrorHandlingHelper.HandleProblems(errors));
        }).WithName("Delete aircraft by id")
        .WithOpenApi();
    }
}
