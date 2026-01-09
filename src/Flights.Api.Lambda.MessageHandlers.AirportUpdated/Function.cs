using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Flights.Infrastructure;
using Flights.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Flights.Api.Lambda.MessageHandlers.AirportUpdated;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    public Function()
    {
        var config = new ConfigurationBuilder().AddEnvironmentVariables(prefix: "FLIGHTS_").Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddDatabaseConnection(config);
        services.AddSingleton<IClock>(SystemClock.Instance);
        _serviceProvider = services.BuildServiceProvider();
    }
    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();
        foreach (var message in evnt.Records)
        {
            try
            {
                await ProcessMessageAsync(message, context);
            }
            catch (Exception ex)
            {
                failures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = message.MessageId
                });
                context.Logger.LogError($"Error processing message {message.MessageId}: {ex}");
            }
        }
        return new SQSBatchResponse
        {
            BatchItemFailures = failures
        };
    }
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processed message {message.Body}");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            context.Logger.LogWarning("Message element is null or empty.");
            return;
        }
        var airportUpdatedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<AirportUpdatedEvent>();
        if (airportUpdatedEvent is null)
        {
            context.Logger.LogWarning("Failed to deserialize AirportUpdatedEvent.");
            return;
        }
        context.Logger.LogInformation($"Processing AirportUpdatedEvent with ID: {airportUpdatedEvent.Id}");
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var airport = await dbContext.Airports.FindAsync(airportUpdatedEvent.AirportId);
        if (airport is null)
        {
            context.Logger.LogWarning($"Airport with ID {airportUpdatedEvent.AirportId} not found. Skipping update.");
            return;
        }
        var clock = _serviceProvider.GetRequiredService<IClock>();
        airport.Update(airportUpdatedEvent.IcaoCode, airportUpdatedEvent.IataCode, airportUpdatedEvent.Name, airportUpdatedEvent.TimeZoneId, clock.GetCurrentInstant());
        await dbContext.SaveChangesAsync();
        context.Logger.LogInformation($"Airport with ID {airportUpdatedEvent.AirportId} updated successfully.");
    }
}
