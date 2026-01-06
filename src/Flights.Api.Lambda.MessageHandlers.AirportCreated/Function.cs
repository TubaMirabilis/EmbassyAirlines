using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Flights.Core.Models;
using Flights.Infrastructure;
using Flights.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Flights.Api.Lambda.MessageHandlers.AirportCreated;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    public Function()
    {
        var config = new ConfigurationBuilder().AddEnvironmentVariables(prefix: "FLIGHTS_").Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
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
                context.Logger.LogError($"Error processing message {message.MessageId}: {ex.Message}");
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
        var airportCreatedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<AirportCreatedEvent>();
        if (airportCreatedEvent is null)
        {
            context.Logger.LogWarning("Failed to deserialize AirportCreatedEvent.");
            return;
        }
        context.Logger.LogInformation($"Processing AirportCreatedEvent with ID: {airportCreatedEvent.Id}");
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Airports.FindAsync(airportCreatedEvent.AirportId) is not null)
        {
            context.Logger.LogInformation($"Airport with ID: {airportCreatedEvent.AirportId} already exists. Skipping creation.");
            return;
        }
        var clock = _serviceProvider.GetRequiredService<IClock>();
        var airport = Airport.Create(new AirportCreationArgs
        {
            CreatedAt = clock.GetCurrentInstant(),
            IataCode = airportCreatedEvent.IataCode,
            IcaoCode = airportCreatedEvent.IcaoCode,
            Id = airportCreatedEvent.AirportId,
            Name = airportCreatedEvent.Name,
            TimeZoneId = airportCreatedEvent.TimeZoneId
        });
        dbContext.Airports.Add(airport);
        await dbContext.SaveChangesAsync();
        context.Logger.LogInformation($"Successfully processed AirportCreatedEvent with ID: {airportCreatedEvent.Id}");
    }
}
