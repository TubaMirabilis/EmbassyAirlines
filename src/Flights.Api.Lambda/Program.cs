using Flights.Api.Lambda;
using Flights.Infrastructure;
using FluentValidation;
using NodaTime;
using Serilog;
using Shared;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
var services = builder.Services;
services.AddEndpoints(assembly);
services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();
if (!builder.Environment.IsEnvironment("FunctionalTests"))
{
    services.AddDatabaseConnection(config);
}
services.AddSingleton<IValidator<ScheduleFlightDto>, ScheduleFlightDtoValidator>();
services.AddOpenApi();
services.AddAWSMessageBus(config);
services.AddSingleton<IClock>(SystemClock.Instance);
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
