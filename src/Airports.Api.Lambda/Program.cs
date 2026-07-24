using Airports.Api.Lambda;
using Airports.Infrastructure;
using Airports.Infrastructure.Database;
using FluentValidation;
using Serilog;
using Shared.Contracts;
using Shared.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
builder.AddHttpApiLambdaDefaults(assembly);
var services = builder.Services;
if (!builder.Environment.IsEnvironment("FunctionalTests"))
{
    services.AddDatabaseConnection(config);
}
services.AddSingleton<IValidator<CreateOrUpdateAirportDto>, CreateOrUpdateAirportDtoValidator>();
services.AddSingleton(TimeProvider.System);
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
await app.ApplyMigrationsAsync<ApplicationDbContext>();
app.MapEndpoints();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
