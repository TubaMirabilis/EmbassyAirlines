using Airports.Api.Lambda;
using Amazon;
using Amazon.DynamoDBv2;
using FluentValidation;
using MassTransit;
using Serilog;
using Shared;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var scope = config["MassTransit:Scope"];
if (string.IsNullOrWhiteSpace(scope))
{
    throw new ArgumentException("MassTransit scope is not configured. Please set the AIRPORTS_MassTransit__Scope environment variable.");
}
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
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: scope));
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(region, h => h.Scope(scope, scopeTopics: true));
        cfg.ConfigureEndpoints(context);
    });
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
