using Airports.Infrastructure.Database;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class UpdateAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("airports/{id}", InvokeAsync)
              .WithSummary("Update an airport")
              .Accepts<CreateOrUpdateAirportDto>("application/json")
              .Produces<AirportDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status409Conflict)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<AirportDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                                                      ILogger<UpdateAirportEndpoint> logger,
                                                                                      IValidator<CreateOrUpdateAirportDto> validator,
                                                                                      TimeProvider timeProvider,
                                                                                      Guid id,
                                                                                      CreateOrUpdateAirportDto dto,
                                                                                      CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for update of airport with id {Id}: {Errors}", id, formattedErrors);
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var airport = await ctx.Airports.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (airport is null)
        {
            logger.LogWarning("Airport with ID {Id} not found", id);
            var error = Error.NotFound("Airport.NotFound", $"Airport with ID {id} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        airport.Update(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId, timeProvider.GetUtcNow());
        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while updating airport {AirportId}", id);
            var error = Error.Conflict("Airport.ConcurrencyConflict", "The airport was modified by another request. Please retrieve its latest state and try again.");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var response = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Ok(response);
    }
}
