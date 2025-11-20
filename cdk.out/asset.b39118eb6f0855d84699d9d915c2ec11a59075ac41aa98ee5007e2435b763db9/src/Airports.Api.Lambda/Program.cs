using Airports.Api.Lambda;
using Amazon;
using Amazon.DynamoDBv2;
using FluentValidation;
using Serilog;
using Shared;
using Shared.Contracts;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2";
var assembly = typeof(Program).Assembly;
builder.Services.AddEndpoints(assembly);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region)));
builder.Services.AddSingleton<IValidator<CreateOrUpdateAirportDto>, CreateOrUpdateAirportDtoValidator>();
builder.Services.AddSingleton<IAirportRepository, AirportRepository>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddAWSMessageBus(bus =>
{
    var airportCreatedTopicArn = config["SNS:AirportCreatedTopicArn"];
    if (string.IsNullOrWhiteSpace(airportCreatedTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for AirportCreatedEvent is not configured.");
    }
    var airportUpdatedTopicArn = config["SNS:AirportUpdatedTopicArn"];
    if (string.IsNullOrWhiteSpace(airportUpdatedTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for AirportUpdatedEvent is not configured.");
    }
    bus.AddSNSPublisher<AirportCreatedEvent>(airportCreatedTopicArn);
    bus.AddSNSPublisher<AirportUpdatedEvent>(airportUpdatedTopicArn);
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapEndpoints();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
