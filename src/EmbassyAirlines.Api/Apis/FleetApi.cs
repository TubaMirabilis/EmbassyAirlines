using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Exceptions;
using EmbassyAirlines.Application.Queries;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace EmbassyAirlines.Api.Apis;

public static class FleetApi
{
    public static WebApplication MapFleetApi(this WebApplication app)
    {
        app.MapGet("/api/fleet", async ([FromServices] IMediator mediator, CancellationToken cancellationToken) =>
        {
            var fleet = await mediator.Send(new GetFleet(), cancellationToken);
            return Results.Ok(fleet);
        }).WithName("Get fleet");
        app.MapGet("/api/fleet/{id}", async ([FromRoute] Guid id, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var aircraft = await mediator.Send(new GetAircraftById(id), cancellationToken);
                return Results.Ok(aircraft);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).WithName("Get aircraft by id");
        app.MapPost("/api/fleet", async ([FromBody] NewAircraftDto dto, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await mediator.Send(dto, cancellationToken);
                return Results.Created($"/api/fleet/{response.Id}", response);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(ex.ValidationError);
            }
        }).WithName("Add aircraft");
        app.MapPut("/api/fleet/{id}", async ([FromRoute] Guid id, [FromBody] UpdateAircraftDto dto, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await mediator.Send(new UpdateAircraft(id, dto), cancellationToken);
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
        }).WithName("Update aircraft");
        app.MapDelete("/api/fleet/{id}", async ([FromRoute] Guid id, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                await mediator.Send(new DeleteAircraft(id), cancellationToken);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).WithName("Delete aircraft");
        return app;
    }
}