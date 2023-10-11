using EaCommon.Errors;
using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Queries;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EmbassyAirlines.Api.Apis;

public static class FleetApi
{
    public static WebApplication MapFleetApi(this WebApplication app)
    {
        app.MapGet("/api/fleet/", async ([FromServices] IMediator mediator, CancellationToken ct) =>
        {
            var fleet = await mediator.Send(new GetFleet(), ct);
            return Results.Ok(fleet);
        }).CacheOutput(x => x.Tag("fleet"))
            .Produces(StatusCodes.Status200OK)
            .WithName("Get fleet");
        app.MapGet("/api/fleet/{id:guid}/", async ([FromRoute] Guid id, [FromServices] IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAircraftById(id), ct);
            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }
            return Results.NotFound();
        }).CacheOutput(x => x.AddPolicy<ByIdCachePolicy>())
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Get aircraft by id");
        app.MapPost("/api/fleet/", async ([FromBody] NewAircraftDto dto, [FromServices] IMediator mediator, IOutputCacheStore cache, CancellationToken ct) =>
        {
            var result = await mediator.Send(dto, ct);
            if (result.IsSuccess)
            {
                await cache.EvictByTagAsync("fleet", ct);
                return Results.Created($"/api/fleet/{result.Value.Id}", result);
            }
            return Results.BadRequest(result.Errors[0].Message);
        }).Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Add aircraft");
        app.MapPut("/api/fleet/{id:guid}/", async ([FromRoute] Guid id, [FromBody] UpdateAircraftDto dto, [FromServices] IMediator mediator, IOutputCacheStore cache, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateAircraft(id, dto), ct);
            if (result.IsSuccess)
            {
                await cache.EvictByTagAsync(id.ToString(), ct);
                return Results.Ok(result.Value);
            }
            var firstError = result.Errors[0];
            if (firstError is ValidationError)
            {
                return Results.BadRequest(firstError.Message);
            }
            return Results.NotFound(firstError.Message);
        }).Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Update aircraft");
        app.MapDelete("/api/fleet/{id:guid}/", async ([FromRoute] Guid id, [FromServices] IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteAircraft(id), ct);
            if (result.IsSuccess)
            {
                return Results.NoContent();
            }
            return Results.NotFound(result.Errors[0].Message);
        }).Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Delete aircraft");
        return app;
    }
}