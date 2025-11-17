using ErrorOr;
using FluentValidation;
using MassTransit;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class UpdateAirportEndpoint : IEndpoint
{
    private readonly IBus _bus;
    private readonly ILogger<UpdateAirportEndpoint> _logger;
    private readonly IAirportRepository _repository;
    private readonly IValidator<CreateOrUpdateAirportDto> _validator;
    public UpdateAirportEndpoint(IBus bus,
                                 ILogger<UpdateAirportEndpoint> logger,
                                 IAirportRepository repository,
                                 IValidator<CreateOrUpdateAirportDto> validator)
    {
        _bus = bus;
        _logger = logger;
        _repository = repository;
        _validator = validator;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("airports/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CreateOrUpdateAirportDto dto, CancellationToken ct)
    {
        var getAirportResult = await _repository.GetAirportByIdAsync(id, ct);
        if (getAirportResult.IsError)
        {
            _logger.LogWarning("Error retrieving airport with id {Id}: {Errors}", id, getAirportResult.FirstError.Description);
            return ErrorHandlingHelper.HandleProblem(getAirportResult.FirstError);
        }
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            _logger.LogWarning("Validation failed for update of airport with id {Id}: {Errors}", id, formattedErrors);
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var airport = getAirportResult.Value;
        airport.Update(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId);
        var updated = await _repository.UpdateAirportAsync(airport, ct);
        if (!updated)
        {
            _logger.LogError("Failed to update airport with id {Id}", id);
            var error = Error.Failure("Airport.Update", $"Failed to update airport with id {id}");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        await _bus.Publish(new AirportUpdatedEvent(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId), ct);
        var response = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Ok(response);
    }
}
