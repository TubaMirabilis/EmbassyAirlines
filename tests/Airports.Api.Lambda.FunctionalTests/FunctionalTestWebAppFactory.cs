using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using AWS.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.DynamoDb;

[assembly: CaptureConsole]
namespace Airports.Api.Lambda.FunctionalTests;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder("amazon/dynamodb-local:3.1.0").Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SNS:AirportCreatedTopicArn", "testAirportCreatedTopicArn");
        builder.UseSetting("SNS:AirportUpdatedTopicArn", "testAirportUpdatedTopicArn");
        builder.UseSetting("DynamoDb:TableName", "airports-test");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAmazonDynamoDB>();
            services.RemoveAll<IMessagePublisher>();
            var credentials = new BasicAWSCredentials("TestAccessKey", "TestSecretKey");
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = _dynamoDbContainer.GetConnectionString()
            };
            services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(credentials, config));
            services.AddSingleton<IMessagePublisher, FakeMessagePublisher>();
        });
    }
    public async ValueTask InitializeAsync()
    {
        await _dynamoDbContainer.StartAsync();
        var client = Services.GetRequiredService<IAmazonDynamoDB>();
        var request = new CreateTableRequest
        {
            TableName = "airports-test",
            KeySchema =
            [
                new KeySchemaElement("Id", KeyType.HASH)
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition("Id", ScalarAttributeType.S)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(1, 1)
        };
        await client.CreateTableAsync(request);
    }
    public new async Task DisposeAsync()
    {
        await _dynamoDbContainer.StopAsync();
        await _dynamoDbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
