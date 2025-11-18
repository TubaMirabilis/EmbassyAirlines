using AWS.Messaging;
using ErrorOr;
using FluentValidation;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class UpdateAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("airports/{id}", InvokeAsync);
    private static async Task<IResult> InvokeAsync(ILogger<UpdateAirportEndpoint> logger,
                                            IMessagePublisher publisher,
                                            IAirportRepository repository,
                                            IValidator<CreateOrUpdateAirportDto> validator,
                                            Guid id,
                                            CreateOrUpdateAirportDto dto,
                                            CancellationToken ct)
    {
        var getAirportResult = await repository.GetAirportByIdAsync(id, ct);
        if (getAirportResult.IsError)
        {
            logger.LogWarning("Error retrieving airport with id {Id}: {Errors}", id, getAirportResult.FirstError.Description);
            return ErrorHandlingHelper.HandleProblem(getAirportResult.FirstError);
        }
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for update of airport with id {Id}: {Errors}", id, formattedErrors);
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var airport = getAirportResult.Value;
        airport.Update(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId);
        var updated = await repository.UpdateAirportAsync(airport, ct);
        if (!updated)
        {
            logger.LogError("Failed to update airport with id {Id}", id);
            var error = Error.Failure("Airport.Update", $"Failed to update airport with id {id}");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        await publisher.PublishAsync(new AirportUpdatedEvent(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId), ct);
        var response = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Ok(response);
    }
}
