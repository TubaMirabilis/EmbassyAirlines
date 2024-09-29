using Flights.Api.Database;
using Flights.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
var services = builder.Services;
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention()
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
}
await app.RunAsync();

public partial class Program { }
