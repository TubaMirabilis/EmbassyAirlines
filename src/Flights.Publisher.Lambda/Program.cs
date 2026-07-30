using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Flights.Infrastructure.Database;
using Flights.Infrastructure.Outbox;
using Flights.Publisher.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Abstractions;
using Shared.Npgsql;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDatabaseConnection<ApplicationDbContext>(config, true, "flights");
builder.Services.AddScoped<IOutboxProcessor, OutboxProcessor>();
builder.Services.AddAWSMessageBus(config);
using var host = builder.Build();

Func<ILambdaContext, Task> handler = async _ =>
{
    await using var scope = host.Services.CreateAsyncScope();
    var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
    await processor.ProcessAsync();
};
await LambdaBootstrapBuilder.Create(handler)
                            .Build()
                            .RunAsync();
