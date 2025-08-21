using Flights.Api;
using Flights.Api.Database;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
var scope = config["MassTransit:Scope"];
if (string.IsNullOrWhiteSpace(scope))
{
    throw new ArgumentException("MassTransit scope is not configured. Please set the FLIGHTS_MassTransit__Scope environment variable.");
}
var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2";
var assembly = typeof(Program).Assembly;
var services = builder.Services;
services.AddEndpoints(assembly);
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();
services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: scope));
    x.SetInMemorySagaRepositoryProvider();
    x.AddConsumers(assembly);
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(region, h => h.Scope(scope, scopeTopics: true));
        cfg.ConfigureEndpoints(context);
        cfg.UseDelayedMessageScheduler();
        cfg.UseDelayedRedelivery(r =>
        {
            r.Handle<Exception>();
            r.Intervals(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30));
        });
    });
});
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config["ConnectionStrings:DefaultConnection"],
               o => o.UseNodaTime())
           .UseSnakeCaseNamingConvention());
services.AddSingleton<IValidator<CreateOrUpdateFlightDto>, CreateOrUpdateFlightDtoValidator>();
services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
    app.MapOpenApi();
}
app.MapEndpoints();
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
