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

namespace Flights.Api.Lambda.MessageHandlers.AircraftCreated;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    public Function()
    {
        var config = new ConfigurationBuilder().AddEnvironmentVariables(prefix: "FLIGHTS_").Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddDatabaseConnection(config);
        _serviceProvider = services.BuildServiceProvider();
    }
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
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
        var aircraftCreatedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<AircraftCreatedEvent>();
        if (aircraftCreatedEvent is null)
        {
            context.Logger.LogWarning("Failed to deserialize AircraftCreatedEvent.");
            return;
        }
        context.Logger.LogInformation($"Processing AircraftCreatedEvent with ID: {aircraftCreatedEvent.Id}");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (await dbContext.Aircraft.FindAsync(aircraftCreatedEvent.AircraftId) is not null)
            {
                context.Logger.LogInformation($"Aircraft with ID: {aircraftCreatedEvent.AircraftId} already exists. Skipping creation.");
                return;
            }
            var aircraft = Aircraft.Create(aircraftCreatedEvent.AircraftId, aircraftCreatedEvent.TailNumber, aircraftCreatedEvent.EquipmentCode, SystemClock.Instance.GetCurrentInstant());
            dbContext.Aircraft.Add(aircraft);
            await dbContext.SaveChangesAsync();
            context.Logger.LogInformation($"Successfully processed AircraftCreatedEvent with ID: {aircraftCreatedEvent.Id}");
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogError($"Error processing AircraftCreatedEvent with ID: {aircraftCreatedEvent.Id}. Exception: {ex.Message}");
        }
    }
}
