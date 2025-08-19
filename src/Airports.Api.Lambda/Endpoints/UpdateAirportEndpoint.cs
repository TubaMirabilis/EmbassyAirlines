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
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly IBus _bus;
    private readonly IConfiguration _config;
    private readonly IValidator<CreateOrUpdateAirportDto> _validator;
    public UpdateAirportEndpoint(IBus bus, IAmazonDynamoDB dynamoDb, IValidator<CreateOrUpdateAirportDto> validator, IConfiguration config)
    {
        _bus = bus;
        _config = config;
        _dynamoDb = dynamoDb;
        _validator = validator;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("airports/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CreateOrUpdateAirportDto dto, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
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
            var error = Error.NotFound("Airport.NotFound", $"Airport with id {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var airportAsDocument = Document.FromAttributeMap(getItemResponse.Item);
        var airportAsJson = airportAsDocument.ToJson();
        var airport = JsonSerializer.Deserialize<Airport>(airportAsJson);
        if (airport is null)
        {
            var error = Error.Failure("Airport.Update", "Failed to deserialize airport");
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
            AttributeUpdates = updatedAirportAsAttributes.ToDictionary(kvp => kvp.Key, kvp => new AttributeValueUpdate { Action = AttributeAction.PUT, Value = kvp.Value })
        };
        var updateItemResponse = await _dynamoDb.UpdateItemAsync(updateItemRequest, ct);
        if (updateItemResponse.HttpStatusCode is not HttpStatusCode.OK)
        {
            var error = Error.Failure("Airport.Update", "Failed to update airport");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        await _bus.Publish(new AirportUpdatedEvent(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId), ct);
        var response = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Ok(response);
    }
}
