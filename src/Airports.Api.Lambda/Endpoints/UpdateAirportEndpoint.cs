using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
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
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly IValidator<CreateOrUpdateAirportDto> _validator;
    private readonly IConfiguration _config;
    private readonly ILogger<UpdateAirportEndpoint> _logger;
    public UpdateAirportEndpoint(IBus bus,
                                 IAmazonDynamoDB dynamoDb,
                                 IValidator<CreateOrUpdateAirportDto> validator,
                                 IConfiguration config,
                                 ILogger<UpdateAirportEndpoint> logger)
    {
        _bus = bus;
        _dynamoDb = dynamoDb;
        _validator = validator;
        _config = config;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("airports/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CreateOrUpdateAirportDto dto, CancellationToken ct)
    {
        var key = new Dictionary<string, AttributeValue>
    {
        { "Id", new AttributeValue { S = id.ToString() } }
    };
        var getItemRequest = new GetItemRequest
        {
            TableName = _config["DynamoDb:TableName"],
            Key = key
        };
        var getItemResponse = await _dynamoDb.GetItemAsync(getItemRequest, ct);
        if (!getItemResponse.IsItemSet)
        {
            _logger.LogWarning("Airport with id {Id} not found", id);
            var error = Error.NotFound("Airport.NotFound", $"Airport with id {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var airportAsDocument = Document.FromAttributeMap(getItemResponse.Item);
        var airportAsJson = airportAsDocument.ToJson();
        var airport = JsonSerializer.Deserialize<Airport>(airportAsJson);
        if (airport is null)
        {
            _logger.LogError("Failed to deserialize airport with id {Id}", id);
            var error = Error.Failure("Airport.Update", $"Failed to deserialize airport with id {id}");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            _logger.LogWarning("Validation failed for update of airport with id {Id}: {Errors}", id, formattedErrors);
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        airport.Update(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId);
        var updatedAirportAsJson = JsonSerializer.Serialize(airport);
        var updatedAirportAsDocument = Document.FromJson(updatedAirportAsJson);
        var updatedAirportAsAttributes = updatedAirportAsDocument.ToAttributeMap();
        updatedAirportAsAttributes.Remove("Id");
        var updateItemRequest = new UpdateItemRequest
        {
            TableName = _config["DynamoDb:TableName"],
            Key = key,
            AttributeUpdates = updatedAirportAsAttributes.ToDictionary(kvp => kvp.Key, kvp =>
            {
                var action = AttributeAction.PUT;
                var value = kvp.Value;
                return new AttributeValueUpdate
                {
                    Action = action,
                    Value = value
                };
            })
        };
        var updateItemResponse = await _dynamoDb.UpdateItemAsync(updateItemRequest, ct);
        if (updateItemResponse.HttpStatusCode is not HttpStatusCode.OK)
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
