using Airports.Api.Lambda;
using Airports.Infrastructure;
using Airports.Infrastructure.Database;
using FluentValidation;
using Serilog;
using Shared.Contracts;
using Shared.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
builder.AddHttpApiLambdaDefaults(assembly);
var services = builder.Services;
if (!builder.Environment.IsDevelopment())
{
    services.AddDatabaseConnection(config);
}
services.AddSingleton<IValidator<CreateOrUpdateAirportDto>, CreateOrUpdateAirportDtoValidator>();
services.AddSingleton(TimeProvider.System);
var app = builder.Build().UseDefaultPipeline().MapDefaultEndpoints();
await app.ApplyMigrationsAsync<ApplicationDbContext>();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
