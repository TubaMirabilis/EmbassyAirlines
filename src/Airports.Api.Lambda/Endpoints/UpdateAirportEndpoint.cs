using AWS.Messaging;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
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
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<AirportDto>, ProblemHttpResult>> InvokeAsync(ILogger<UpdateAirportEndpoint> logger,
                                                                                      IMessagePublisher publisher,
                                                                                      IAirportRepository repository,
                                                                                      IValidator<CreateOrUpdateAirportDto> validator,
                                                                                      TimeProvider timeProvider,
                                                                                      Guid id,
                                                                                      CreateOrUpdateAirportDto dto,
                                                                                      CancellationToken ct)
    {
        var getAirportResult = await repository.GetAirportByIdAsync(id, ct);
        if (getAirportResult.IsError)
        {
            logger.LogWarning("Error retrieving airport with id {Id}: {Errors}", id, getAirportResult.FirstError.Description);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(getAirportResult.FirstError));
        }
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for update of airport with id {Id}: {Errors}", id, formattedErrors);
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var airport = getAirportResult.Value;
        airport.Update(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId, timeProvider.GetUtcNow());
        var updated = await repository.UpdateAirportAsync(airport, ct);
        if (!updated)
        {
            logger.LogError("Failed to update airport with id {Id}", id);
            var error = Error.Failure("Airport.Update", $"Failed to update airport with id {id}");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var evnt = new AirportUpdatedEvent(Guid.NewGuid(), airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        await publisher.PublishAsync(evnt, ct);
        var response = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Ok(response);
    }
}
