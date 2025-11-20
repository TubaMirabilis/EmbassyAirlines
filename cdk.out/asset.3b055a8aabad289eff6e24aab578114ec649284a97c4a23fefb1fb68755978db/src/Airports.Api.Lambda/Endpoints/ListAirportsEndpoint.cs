using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Shared.Contracts;
using Shared.Endpoints;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class ListAirportsEndpoint : IEndpoint
{
    private readonly IConfiguration _config;
    private readonly IAmazonDynamoDB _dynamoDb;
    public ListAirportsEndpoint(IConfiguration config, IAmazonDynamoDB dynamoDb)
    {
        _config = config;
        _dynamoDb = dynamoDb;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports", InvokeAsync);
    private async Task<IResult> InvokeAsync(CancellationToken ct)
    {
        var scanRequest = new ScanRequest
        {
            TableName = _config["DynamoDb:TableName"]
        };
        var response = await _dynamoDb.ScanAsync(scanRequest, ct);
        var airports = response.Items.Select(item =>
        {
            var itemAsDocument = Document.FromAttributeMap(item);
            var airportAsJson = itemAsDocument.ToJson();
            return JsonSerializer.Deserialize<AirportDto>(airportAsJson);
        });
        return TypedResults.Ok(airports);
    }
}
