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

internal sealed class CreateAirportEndpoint : IEndpoint
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly IBus _bus;
    private readonly IConfiguration _config;
    private readonly IValidator<CreateOrUpdateAirportDto> _validator;
    public CreateAirportEndpoint(IBus bus, IAmazonDynamoDB dynamoDb, IValidator<CreateOrUpdateAirportDto> validator, IConfiguration config)
    {
        _bus = bus;
        _config = config;
        _dynamoDb = dynamoDb;
        _validator = validator;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("airports", InvokeAsync);
    private async Task<IResult> InvokeAsync(CreateOrUpdateAirportDto dto, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var scanRequest = new ScanRequest
        {
            TableName = _config["DynamoDb:TableName"],
            FilterExpression = "IataCode = :iataCode",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":iataCode", new AttributeValue { S = dto.IataCode } }
        }
        };
        var scanResponse = await _dynamoDb.ScanAsync(scanRequest, ct);
        if (scanResponse.Items.Count > 0)
        {
            var error = Error.Conflict("Airport.Conflict", $"Airport with IATA code {dto.IataCode} already exists");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var airport = Airport.Create(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId);
        var airportAsJson = JsonSerializer.Serialize(airport);
        var itemAsDocument = Document.FromJson(airportAsJson);
        var itemAsAttributes = itemAsDocument.ToAttributeMap();
        var createItemRequest = new PutItemRequest
        {
            TableName = _config["DynamoDb:TableName"],
            Item = itemAsAttributes
        };
        var response = await _dynamoDb.PutItemAsync(createItemRequest, ct);
        if (response.HttpStatusCode is not HttpStatusCode.OK)
        {
            var error = Error.Failure("Airport.Create", "Failed to create airport");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        await _bus.Publish(new AirportCreatedEvent(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId), ct);
        return TypedResults.Created($"/airports/{airport.Id}", airport);
    }
}
