using System.Net;
using System.Text.Json;
using Airports.Api.Lambda;
using Airports.Api.Lambda.Contracts;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
var tableName = config["DynamoDb:TableName"];
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
var app = builder.Build();
app.MapGet("airports", async ([FromServices] IAmazonDynamoDB dynamoDb) =>
{
    var scanRequest = new ScanRequest
    {
        TableName = tableName
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
        TableName = tableName,
        Key = key
    };
    var response = await dynamoDb.GetItemAsync(getItemRequest);
    if (response.Item is null)
    {
        return Results.NotFound();
    }
    var itemAsDocument = Document.FromAttributeMap(response.Item);
    var airportAsJson = itemAsDocument.ToJson();
    var airport = JsonSerializer.Deserialize<AirportDto>(airportAsJson);
    return Results.Ok(airport);
});
app.MapPost("airports", async ([FromServices] IAmazonDynamoDB dynamoDb, IBus bus, [FromBody] CreateAirportDto dto) =>
{
    var airport = Airport.Create(dto.IataCode, dto.Name, dto.TimeZoneId);
    var airportAsJson = JsonSerializer.Serialize(airport);
    var itemAsDocument = Document.FromJson(airportAsJson);
    var itemAsAttributes = itemAsDocument.ToAttributeMap();
    var createItemRequest = new PutItemRequest
    {
        TableName = tableName,
        Item = itemAsAttributes
    };
    var response = await dynamoDb.PutItemAsync(createItemRequest);
    if (response.HttpStatusCode is not HttpStatusCode.OK)
    {
        return Results.BadRequest("Failed to create airport");
    }
    await bus.Publish(new AirportCreatedEvent(airport.Id, airport.Name, airport.IataCode, airport.TimeZoneId));
    return Results.Created($"/airports/{airport.Id}", airport);
});
app.MapPut("airports/{id}", async ([FromServices] IAmazonDynamoDB dynamoDb, IBus bus, [FromRoute] Guid id, [FromBody] UpdateAirportDto dto) =>
{
    var key = new Dictionary<string, AttributeValue>
    {
        { "Id", new AttributeValue { S = id.ToString() } }
    };
    var getItemRequest = new GetItemRequest
    {
        TableName = tableName,
        Key = key
    };
    var getItemResponse = await dynamoDb.GetItemAsync(getItemRequest);
    if (getItemResponse.Item is null)
    {
        return Results.NotFound();
    }
    var airportAsDocument = Document.FromAttributeMap(getItemResponse.Item);
    var airportAsJson = airportAsDocument.ToJson();
    var airport = JsonSerializer.Deserialize<Airport>(airportAsJson);
    if (airport is null)
    {
        return Results.BadRequest("Failed to deserialize airport");
    }
    airport.Update(dto.IataCode, dto.Name, dto.TimeZoneId);
    var updatedAirportAsJson = JsonSerializer.Serialize(airport);
    var updatedAirportAsDocument = Document.FromJson(updatedAirportAsJson);
    var updatedAirportAsAttributes = updatedAirportAsDocument.ToAttributeMap();
    var updateItemRequest = new UpdateItemRequest
    {
        TableName = tableName,
        Key = key,
        AttributeUpdates = updatedAirportAsAttributes.ToDictionary(kvp => kvp.Key, kvp => new AttributeValueUpdate { Action = AttributeAction.PUT, Value = kvp.Value })
    };
    var updateItemResponse = await dynamoDb.UpdateItemAsync(updateItemRequest);
    if (updateItemResponse.HttpStatusCode is not HttpStatusCode.OK)
    {
        return Results.BadRequest("Failed to update airport");
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
        TableName = tableName,
        Key = key
    };
    var response = await dynamoDb.DeleteItemAsync(deleteItemRequest);
    if (response.HttpStatusCode is not HttpStatusCode.OK)
    {
        return Results.BadRequest("Failed to delete airport");
    }
    await bus.Publish(new AirportDeletedEvent(id));
    return Results.NoContent();
});
app.UseExceptionHandler();
await app.RunAsync();
