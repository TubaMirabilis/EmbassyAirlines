using Flights.Api.Lambda;
using Flights.Api.Lambda.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2";
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config["ConnectionStrings:DefaultConnection"],
               o => o.UseNodaTime())
           .UseSnakeCaseNamingConvention());
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: config["MassTransit:Scope"]));
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(region, h => h.Scope(config["MassTransit:Scope"], scopeTopics: true));
        cfg.ConfigureEndpoints(context);
    });
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
}
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
