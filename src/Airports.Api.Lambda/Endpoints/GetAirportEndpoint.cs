using Microsoft.AspNetCore.Http.HttpResults;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class GetAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports/{id}", InvokeAsync)
              .WithSummary("Get an airport by ID")
              .Produces<AirportDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<AirportDto>, ProblemHttpResult>> InvokeAsync(IAirportRepository repository, ILogger<GetAirportEndpoint> logger, Guid id, CancellationToken ct)
    {
        var getAirportResult = await repository.GetAirportByIdAsync(id, ct);
        if (getAirportResult.IsError)
        {
            logger.LogWarning("Error retrieving airport with id {Id}: {Errors}", id, getAirportResult.FirstError.Description);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(getAirportResult.FirstError));
        }
        var airport = getAirportResult.Value;
        var response = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Ok(response);
    }
}
