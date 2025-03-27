using System.Net;
using System.Text.Json;
using Airports.Api.Lambda;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ErrorOr;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Contracts;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2";
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region)));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: config["MassTransit:Scope"]));
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(region, h => h.Scope(config["MassTransit:Scope"], scopeTopics: true));
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddSingleton<IValidator<CreateOrUpdateAirportDto>, CreateOrUpdateAirportDtoValidator>();
var app = builder.Build();
app.MapGet("airports", async ([FromServices] IAmazonDynamoDB dynamoDb) =>
{
    var scanRequest = new ScanRequest
    {
        TableName = config["DynamoDb:TableName"]
    };
    var response = await dynamoDb.ScanAsync(scanRequest);
    var airports = response.Items.Select(item =>
    {
        var itemAsDocument = Document.FromAttributeMap(item);
        var airportAsJson = itemAsDocument.ToJson();
        return JsonSerializer.Deserialize<AirportDto>(airportAsJson);
    });
    return Results.Ok(airports);
});
app.MapGet("airports/{id}", async ([FromServices] IAmazonDynamoDB dynamoDb, [FromRoute] Guid id) =>
{
    var key = new Dictionary<string, AttributeValue>
    {
        { "Id", new AttributeValue { S = id.ToString() } }
    };
    var getItemRequest = new GetItemRequest
    {
        TableName = config["DynamoDb:TableName"],
        Key = key
    };
    var response = await dynamoDb.GetItemAsync(getItemRequest);
    if (!response.IsItemSet)
    {
        var error = Error.NotFound("Airport.NotFound", $"Airport with id {id} not found");
        return ErrorHandlingHelper.HandleProblem(error);
    }
    var itemAsDocument = Document.FromAttributeMap(response.Item);
    var airportAsJson = itemAsDocument.ToJson();
    var airport = JsonSerializer.Deserialize<AirportDto>(airportAsJson);
    return Results.Ok(airport);
});
app.MapPost("airports", async ([FromServices] IAmazonDynamoDB dynamoDb, IBus bus, IValidator<CreateOrUpdateAirportDto> validator,
                               [FromBody] CreateOrUpdateAirportDto dto) =>
{
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid(out var formattedErrors))
    {
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
    var scanResponse = await dynamoDb.ScanAsync(scanRequest);
    if (scanResponse.Items.Count > 0)
    {
        var error = Error.Conflict("Airport.Conflict", $"Airport with IATA code {dto.IataCode} already exists");
        return ErrorHandlingHelper.HandleProblem(error);
    }
    var airport = Airport.Create(dto.IataCode, dto.Name, dto.TimeZoneId);
    var airportAsJson = JsonSerializer.Serialize(airport);
    var itemAsDocument = Document.FromJson(airportAsJson);
    var itemAsAttributes = itemAsDocument.ToAttributeMap();
    var createItemRequest = new PutItemRequest
    {
        TableName = config["DynamoDb:TableName"],
        Item = itemAsAttributes
    };
    var response = await dynamoDb.PutItemAsync(createItemRequest);
    if (response.HttpStatusCode is not HttpStatusCode.OK)
    {
        var error = Error.Failure("Airport.Create", "Failed to create airport");
        return ErrorHandlingHelper.HandleProblem(error);
    }
    await bus.Publish(new AirportCreatedEvent(airport.Id, airport.Name, airport.IataCode, airport.TimeZoneId));
    return Results.Created($"/airports/{airport.Id}", airport);
});
app.MapPut("airports/{id}", async ([FromServices] IAmazonDynamoDB dynamoDb, IBus bus, [FromRoute] Guid id, IValidator<CreateOrUpdateAirportDto> validator,
                                   [FromBody] CreateOrUpdateAirportDto dto) =>
{
    var validationResult = await validator.ValidateAsync(dto);
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
        TableName = config["DynamoDb:TableName"],
        Key = key
    };
    var getItemResponse = await dynamoDb.GetItemAsync(getItemRequest);
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
    airport.Update(dto.IataCode, dto.Name, dto.TimeZoneId);
    var updatedAirportAsJson = JsonSerializer.Serialize(airport);
    var updatedAirportAsDocument = Document.FromJson(updatedAirportAsJson);
    var updatedAirportAsAttributes = updatedAirportAsDocument.ToAttributeMap();
    updatedAirportAsAttributes.Remove("Id");
    var updateItemRequest = new UpdateItemRequest
    {
        TableName = config["DynamoDb:TableName"],
        Key = key,
        AttributeUpdates = updatedAirportAsAttributes.ToDictionary(kvp => kvp.Key, kvp => new AttributeValueUpdate { Action = AttributeAction.PUT, Value = kvp.Value })
    };
    var updateItemResponse = await dynamoDb.UpdateItemAsync(updateItemRequest);
    if (updateItemResponse.HttpStatusCode is not HttpStatusCode.OK)
    {
        var error = Error.Failure("Airport.Update", "Failed to update airport");
        return ErrorHandlingHelper.HandleProblem(error);
    }
    await bus.Publish(new AirportUpdatedEvent(airport.Id, airport.Name, airport.IataCode, airport.TimeZoneId));
    var response = new AirportDto(airport.Id, airport.Name, airport.IataCode, airport.TimeZoneId);
    return Results.Ok(response);
});
app.MapDelete("airports/{id}", async ([FromServices] IAmazonDynamoDB dynamoDb, IBus bus, [FromRoute] Guid id) =>
{
    var key = new Dictionary<string, AttributeValue>
    {
        { "Id", new AttributeValue { S = id.ToString() } }
    };
    var deleteItemRequest = new DeleteItemRequest
    {
        TableName = config["DynamoDb:TableName"],
        Key = key
    };
    var response = await dynamoDb.DeleteItemAsync(deleteItemRequest);
    if (response.HttpStatusCode is not HttpStatusCode.OK)
    {
        var error = Error.Failure("Airport.Delete", "Failed to delete airport");
        return ErrorHandlingHelper.HandleProblem(error);
    }
    await bus.Publish(new AirportDeletedEvent(id));
    return Results.NoContent();
});
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
