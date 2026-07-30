using Flights.Api.Lambda;
using Flights.Infrastructure.Database;
using FluentValidation;
using NodaTime;
using Serilog;
using Shared.Contracts;
using Shared.Npgsql;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
builder.AddHttpApiLambdaDefaults(assembly);
var services = builder.Services;
if (!builder.Environment.IsDevelopment())
{
    services.AddDatabaseConnection<ApplicationDbContext>(config, true, "flights");
}
services.AddSingleton<IValidator<ScheduleFlightDto>, ScheduleFlightDtoValidator>();
services.AddSingleton<IClock>(SystemClock.Instance);
services.AddScoped<FlightScheduler>();
var app = builder.Build().UseDefaultPipeline().MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync<ApplicationDbContext>();
}
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
