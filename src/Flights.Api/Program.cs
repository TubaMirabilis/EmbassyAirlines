using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention().UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
var app = builder.Build();
await app.RunAsync();

public partial class Program { }
