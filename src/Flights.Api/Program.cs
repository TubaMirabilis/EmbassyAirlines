using System.Reflection;
using Flights.Api.Database;
using Flights.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
var services = builder.Services;
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config["ConnectionStrings:DefaultConnection"],
               o => o.UseNodaTime())
           .UseSnakeCaseNamingConvention()
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
services.AddEndpoints(Assembly.GetExecutingAssembly());
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
}
app.MapEndpoints();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
await app.RunAsync();

public partial class Program { }
