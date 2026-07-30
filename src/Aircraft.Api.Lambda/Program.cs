using Aircraft.Api.Lambda;
using Aircraft.Infrastructure;
using Aircraft.Infrastructure.Database;
using Amazon.S3;
using FluentValidation;
using Serilog;
using Shared.Contracts;
using Shared.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRCRAFT_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
builder.AddHttpApiLambdaDefaults(assembly);
var services = builder.Services;
if (!builder.Environment.IsDevelopment())
{
    services.AddDatabaseConnection(config);
}
services.AddAWSService<IAmazonS3>();
services.AddSingleton<IValidator<CreateAircraftDto>, CreateAircraftDtoValidator>();
services.AddSingleton(TimeProvider.System);
var app = builder.Build().UseDefaultPipeline().MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync<ApplicationDbContext>();
}
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
