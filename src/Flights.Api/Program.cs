using Flights.Api.Database;
using Flights.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
var services = builder.Services;
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config["ConnectionStrings:DefaultConnection"])
           .UseSnakeCaseNamingConvention()
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
}
await app.RunAsync();

public partial class Program { }
