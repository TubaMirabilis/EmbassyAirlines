using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ErrorOr;
using MassTransit;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class DeleteAirportEndpoint : IEndpoint
{
    private readonly IBus _bus;
    private readonly IConfiguration _config;
    private readonly IAmazonDynamoDB _dynamoDb;
    public DeleteAirportEndpoint(IBus bus, IConfiguration config, IAmazonDynamoDB dynamoDb)
    {
        _bus = bus;
        _config = config;
        _dynamoDb = dynamoDb;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapDelete("airports/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CancellationToken ct)
    {
        var key = new Dictionary<string, AttributeValue>
    {
        { "Id", new AttributeValue { S = id.ToString() } }
    };
        var deleteItemRequest = new DeleteItemRequest
        {
            TableName = _config["DynamoDb:TableName"],
            Key = key
        };
        var response = await _dynamoDb.DeleteItemAsync(deleteItemRequest, ct);
        if (response.HttpStatusCode is not HttpStatusCode.OK)
        {
            var error = Error.Failure("Airport.Delete", "Failed to delete airport");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        await _bus.Publish(new AirportDeletedEvent(id), ct);
        return TypedResults.NoContent();
    }
}
