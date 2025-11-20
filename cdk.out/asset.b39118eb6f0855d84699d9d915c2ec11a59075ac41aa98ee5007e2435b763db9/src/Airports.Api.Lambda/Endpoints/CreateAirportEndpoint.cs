using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using AWS.Messaging;
using ErrorOr;
using FluentValidation;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class CreateAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("airports", InvokeAsync);
    private async Task<IResult> InvokeAsync(IConfiguration config,
                                            IAmazonDynamoDB dynamoDb,
                                            ILogger<CreateAirportEndpoint> logger,
                                            IMessagePublisher publisher,
                                            IValidator<CreateOrUpdateAirportDto> validator,
                                            CreateOrUpdateAirportDto dto,
                                            CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for creation of airport: {Errors}", formattedErrors);
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var scanRequest = new ScanRequest
        {
            TableName = config["DynamoDb:TableName"],
            FilterExpression = "IataCode = :iataCode",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":iataCode", new AttributeValue { S = dto.IataCode } }
        }
        };
        var scanResponse = await dynamoDb.ScanAsync(scanRequest, ct);
        if (scanResponse.Items.Count > 0)
        {
            logger.LogWarning("Conflict: Airport with IATA code {IataCode} already exists", dto.IataCode);
            var error = Error.Conflict("Airport.Conflict", $"Airport with IATA code {dto.IataCode} already exists");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var airport = Airport.Create(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId);
        var airportAsJson = JsonSerializer.Serialize(airport);
        var itemAsDocument = Document.FromJson(airportAsJson);
        var itemAsAttributes = itemAsDocument.ToAttributeMap();
        var createItemRequest = new PutItemRequest
        {
            TableName = config["DynamoDb:TableName"],
            Item = itemAsAttributes
        };
        var response = await dynamoDb.PutItemAsync(createItemRequest, ct);
        if (response.HttpStatusCode is not HttpStatusCode.OK)
        {
            logger.LogError("Failed to create airport: {Errors}", response);
            var error = Error.Failure("Airport.Create", "Failed to create airport");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        await publisher.PublishAsync(new AirportCreatedEvent(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId), ct);
        return TypedResults.Created($"/airports/{airport.Id}", airport);
    }
}
