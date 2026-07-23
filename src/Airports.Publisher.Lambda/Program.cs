using Airports.Infrastructure;
using Airports.Infrastructure.Outbox;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;
using Shared.Abstractions;
using Shared.Contracts;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDatabaseConnection(config);
builder.Services.AddScoped<IOutboxProcessor, OutboxProcessor>();
builder.Services.AddAWSMessageBus(bus =>
{
    var airportCreatedTopicArn = config["SNS:AirportCreatedTopicArn"];
    var airportUpdatedTopicArn = config["SNS:AirportUpdatedTopicArn"];
    Ensure.NotNullOrEmpty(airportCreatedTopicArn);
    Ensure.NotNullOrEmpty(airportUpdatedTopicArn);
    bus.AddSNSPublisher<AirportCreatedEvent>(airportCreatedTopicArn);
    bus.AddSNSPublisher<AirportUpdatedEvent>(airportUpdatedTopicArn);
});
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
