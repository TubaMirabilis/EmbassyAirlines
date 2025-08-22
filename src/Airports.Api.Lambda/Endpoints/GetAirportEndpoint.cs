using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ErrorOr;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class GetAirportEndpoint : IEndpoint
{
    private readonly IConfiguration _config;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<GetAirportEndpoint> _logger;
    public GetAirportEndpoint(IConfiguration config, IAmazonDynamoDB dynamoDb, ILogger<GetAirportEndpoint> logger)
    {
        _config = config;
        _dynamoDb = dynamoDb;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CancellationToken ct)
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
        var response = await _dynamoDb.GetItemAsync(getItemRequest, ct);
        if (!response.IsItemSet)
        {
            _logger.LogWarning("Airport with id {Id} not found", id);
            var error = Error.NotFound("Airport.NotFound", $"Airport with id {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var itemAsDocument = Document.FromAttributeMap(response.Item);
        var airportAsJson = itemAsDocument.ToJson();
        var airport = JsonSerializer.Deserialize<AirportDto>(airportAsJson);
        return TypedResults.Ok(airport);
    }
}
