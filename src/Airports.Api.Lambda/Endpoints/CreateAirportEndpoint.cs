using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using AWS.Messaging;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class CreateAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("airports", InvokeAsync)
              .WithSummary("Create an airport")
              .Accepts<CreateOrUpdateAirportDto>("application/json")
              .Produces<AirportDto>(StatusCodes.Status201Created)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status409Conflict)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private async Task<Results<Created<AirportDto>, ProblemHttpResult>> InvokeAsync(IConfiguration config,
                                                                                    IAmazonDynamoDB dynamoDb,
                                                                                    ILogger<CreateAirportEndpoint> logger,
                                                                                    IMessagePublisher publisher,
                                                                                    IValidator<CreateOrUpdateAirportDto> validator,
                                                                                    TimeProvider timeProvider,
                                                                                    CreateOrUpdateAirportDto dto,
                                                                                    CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for creation of airport: {Errors}", formattedErrors);
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
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
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var airport = Airport.Create(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId, timeProvider.GetUtcNow());
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
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var evnt = new AirportCreatedEvent(Guid.NewGuid(), airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        await publisher.PublishAsync(evnt, ct);
        var body = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Created($"/airports/{airport.Id}", body);
    }
}
