using Aircraft.Api.Lambda;
using Aircraft.Infrastructure;
using Amazon.S3;
using FluentValidation;
using Serilog;
using Shared;
using Shared.Contracts;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRCRAFT_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
builder.Services.AddEndpoints(assembly);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddProblemDetails();
if (!builder.Environment.IsEnvironment("FunctionalTests"))
{
    builder.Services.AddDatabaseConnection(config);
}
builder.Services.AddSingleton<IValidator<CreateAircraftDto>, CreateAircraftDtoValidator>();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAWSMessageBus(bus =>
{
    var aircraftCreatedTopicArn = config["SNS:AircraftCreatedTopicArn"];
    Ensure.NotNullOrEmpty(aircraftCreatedTopicArn);
    bus.AddSNSPublisher<AircraftCreatedEvent>(aircraftCreatedTopicArn);
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
await app.ApplyMigrationsAsync();
app.MapEndpoints();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
