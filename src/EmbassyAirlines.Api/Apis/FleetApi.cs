using EaCommon.Exceptions;
using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Exceptions;
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
            try
            {
                var aircraft = await mediator.Send(new GetAircraftById(id), ct);
                return Results.Ok(aircraft);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).CacheOutput(x => x.AddPolicy<ByIdCachePolicy>())
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Get aircraft by id");
        app.MapPost("/api/fleet/", async ([FromBody] NewAircraftDto dto, [FromServices] IMediator mediator, IOutputCacheStore cache, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.Send(dto, ct);
                await cache.EvictByTagAsync("fleet", ct);
                return Results.Created($"/api/fleet/{response.Id}", response);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(ex.ValidationError);
            }
        }).Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Add aircraft");
        app.MapPut("/api/fleet/{id:guid}/", async ([FromRoute] Guid id, [FromBody] UpdateAircraftDto dto, [FromServices] IMediator mediator, IOutputCacheStore cache, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.Send(new UpdateAircraft(id, dto), ct);
                await cache.EvictByTagAsync(id.ToString(), ct);
                return Results.Ok(response);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(ex.ValidationError);
            }
        }).Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Update aircraft");
        app.MapDelete("/api/fleet/{id:guid}/", async ([FromRoute] Guid id, [FromServices] IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.Send(new DeleteAircraft(id), ct);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Delete aircraft");
        return app;
    }
}