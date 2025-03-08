using System.Text.Json;
using Amazon.DynamoDBv2;
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
            services.AddScoped<JsonSerializerOptions>(_ => new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            services.RemoveAll<IAmazonDynamoDB>();
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = _dynamoDbContainer.GetConnectionString()
            };
            services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(config));
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
