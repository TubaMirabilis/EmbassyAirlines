using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class GetAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports/{id}", InvokeAsync);
    static private async Task<IResult> InvokeAsync(IAirportRepository repository, ILogger<GetAirportEndpoint> logger, Guid id, CancellationToken ct)
    {
        var getAirportResult = await repository.GetAirportByIdAsync(id, ct);
        if (getAirportResult.IsError)
        {
            logger.LogWarning("Error retrieving airport with id {Id}: {Errors}", id, getAirportResult.FirstError.Description);
            return ErrorHandlingHelper.HandleProblem(getAirportResult.FirstError);
        }
        var airport = getAirportResult.Value;
        var response = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Ok(response);
    }
}
