using System.Text.Json;
using Airports.Api.Lambda.FunctionalTests.Extensions;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.DynamoDb;

namespace Airports.Api.Lambda.FunctionalTests;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder().Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var configuration = new Dictionary<string, string?>
            {
                                {"DynamoDb:TableName", "airports"},
                                {"MassTransit:Scope", "embassy-airlines"}
            };
            config.AddInMemoryCollection(configuration);
        });
        builder.ConfigureTestServices(services =>
        {
            var credentials = new BasicAWSCredentials("test-access-key", "test-secret-key");
            services.AddScoped<JsonSerializerOptions>(_ => new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var descriptors = services.Where(d => d.IsMassTransitService())
.ToList();
            foreach (var d in descriptors)
            {
                services.Remove(d);
            }
            services.AddMassTransitTestHarness();
            services.RemoveAll<IAmazonDynamoDB>();
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = _dynamoDbContainer.GetConnectionString()
            };
            services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(credentials, config));
        });
    }
    public Task InitializeAsync() => _dynamoDbContainer.StartAsync();
    public new async Task DisposeAsync()
    {
        await _dynamoDbContainer.StopAsync();
        await _dynamoDbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
