using Airports.Api.Lambda;
using Amazon;
using Amazon.DynamoDBv2;
using FluentValidation;
using MassTransit;
using Shared;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
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
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: scope));
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(region, h => h.Scope(scope, scopeTopics: true));
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddSingleton<IValidator<CreateOrUpdateAirportDto>, CreateOrUpdateAirportDtoValidator>();
builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapEndpoints();
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
