using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ErrorOr;

namespace Airports.Api.Lambda;

internal sealed class AirportRepository : IAirportRepository
{
    private readonly IConfiguration _config;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<AirportRepository> _logger;
    public AirportRepository(IConfiguration config, IAmazonDynamoDB dynamoDb, ILogger<AirportRepository> logger)
    {
        _config = config;
        _dynamoDb = dynamoDb;
        _logger = logger;
    }
    public async Task<ErrorOr<Airport>> GetAirportByIdAsync(Guid id, CancellationToken ct)
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
            return Error.NotFound("Airport.NotFound", $"Airport with id {id} not found");
        }
        var airportAsDocument = Document.FromAttributeMap(getItemResponse.Item);
        var airportAsJson = airportAsDocument.ToJson();
        var airport = JsonSerializer.Deserialize<Airport>(airportAsJson);
        if (airport is null)
        {
            _logger.LogError("Failed to deserialize airport with id {Id}", id);
            return Error.Failure("Airport.Update", $"Failed to deserialize airport with id {id}");
        }
        return airport;
    }
    public async Task<bool> UpdateAirportAsync(Airport airport, CancellationToken ct)
    {
        var updatedAirportAsJson = JsonSerializer.Serialize(airport);
        var updatedAirportAsDocument = Document.FromJson(updatedAirportAsJson);
        var updatedAirportAsAttributes = updatedAirportAsDocument.ToAttributeMap();
        updatedAirportAsAttributes.Remove("Id");
        var key = new Dictionary<string, AttributeValue>
    {
        { "Id", new AttributeValue { S = airport.Id.ToString() } }
    };
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
        if (updateItemResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Failed to update airport with id {Id}. Status code: {StatusCode}", airport.Id, updateItemResponse.HttpStatusCode);
        }
        return updateItemResponse.HttpStatusCode is HttpStatusCode.OK;
    }
}
