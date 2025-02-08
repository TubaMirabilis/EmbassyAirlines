using System.Reflection;
using Flights.Api;
using Flights.Api.Database;
using Flights.Api.Domain.Journeys;
using Flights.Api.Domain.Seats;
using Flights.Api.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared;
using Shared.Extensions;
using Shared.Middleware;
using Sqids;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
var awsOptions = config.GetAWSOptions();
var services = builder.Services;
services.AddDefaultAWSOptions(awsOptions);
services.AddOpenApi();
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config["ConnectionStrings:DefaultConnection"],
               o => o.UseNodaTime())
           .UseSnakeCaseNamingConvention());
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
services.AddValidatorsFromAssemblyContaining<Program>();
services.AddSingleton<IJourneyService, JourneyService>();
services.AddSingleton<ISeatService, SeatService>();
services.AddSingleton(new SqidsEncoder<int>(new()
{
    Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789",
    MinLength = 6
}));
services.AddMassTransit(config);
services.AddEndpoints(Assembly.GetExecutingAssembly());
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
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
