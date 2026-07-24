using Aircraft.Api.Lambda;
using Aircraft.Infrastructure;
using Aircraft.Infrastructure.Database;
using Amazon.S3;
using FluentValidation;
using Serilog;
using Shared.Contracts;
using Shared.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRCRAFT_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
builder.AddHttpApiLambdaDefaults(assembly);
var services = builder.Services;
if (!builder.Environment.IsEnvironment("FunctionalTests"))
{
    services.AddDatabaseConnection(config);
}
services.AddAWSService<IAmazonS3>();
services.AddSingleton<IValidator<CreateAircraftDto>, CreateAircraftDtoValidator>();
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
