using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ErrorOr;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class ListAirportsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports", InvokeAsync)
              .WithSummary("List all airports")
              .Produces<IEnumerable<AirportDto>>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private async Task<Results<Ok<IEnumerable<AirportDto>>, ProblemHttpResult>> InvokeAsync(IConfiguration config, IAmazonDynamoDB dynamoDb, CancellationToken ct)
    {
        var scanRequest = new ScanRequest
        {
            TableName = config["DynamoDb:TableName"]
        };
        var response = await dynamoDb.ScanAsync(scanRequest, ct);
        try
        {
            var airports = response.Items.Select(item =>
            {
                var itemAsDocument = Document.FromAttributeMap(item);
                var airportAsJson = itemAsDocument.ToJson();
                var airport = JsonSerializer.Deserialize<AirportDto>(airportAsJson) ?? throw new JsonException("Deserialization resulted in null");
                return airport;
            });
            return TypedResults.Ok(airports);
        }
        catch (JsonException)
        {
            var error = Error.Unexpected("Airport.ListingError", "An error occurred while processing the airport data.");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
    }
}
