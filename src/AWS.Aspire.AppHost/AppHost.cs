using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Aspire.Hosting.AWS.Lambda;

var builder = DistributedApplication.CreateBuilder(args);
var awsSdkConfig = builder.AddAWSSDKConfig().WithRegion(RegionEndpoint.EUWest2);
var awsResources = builder.AddAWSCDKStack("AirportsServiceAWSResources").WithReference(awsSdkConfig);
var dynamoDb = builder.AddAWSDynamoDBLocal("DynamoDBAirports");
builder.Eventing.Subscribe<ResourceReadyEvent>(dynamoDb.Resource, async (evnt, ct) =>
{
    var serviceUrl = dynamoDb.Resource.GetEndpoint("http").Url;
    var ddbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = serviceUrl });
    await ddbClient.CreateTableAsync(new CreateTableRequest
    {
        TableName = "airports",
        KeySchema =
            [
                new KeySchemaElement("Id", KeyType.HASH)
            ],
        AttributeDefinitions =
            [
                new AttributeDefinition("Id", ScalarAttributeType.S)
            ],
        ProvisionedThroughput = new ProvisionedThroughput(1, 1)
    }, ct);
});
var topic1 = awsResources.AddSNSTopic("AirportCreatedTopic").AddOutput("AirportCreatedTopicArn", t => t.TopicArn);
var topic2 = awsResources.AddSNSTopic("AirportUpdatedTopic").AddOutput("AirportUpdatedTopicArn", t => t.TopicArn);
builder.AddAWSLambdaServiceEmulator();
var airportsApiLambda = builder.AddAWSLambdaFunction<Projects.Airports_Api_Lambda>("airports-api", "Airports.Api.Lambda")
                               .WaitFor(dynamoDb)
                               .WaitFor(topic1)
                               .WaitFor(topic2)
                               .WithReference(awsSdkConfig)
                               .WithReference(dynamoDb)
                               .WithReference(topic1)
                               .WithReference(topic2)
                               .WithEnvironment("AIRPORTS_SNS__AirportCreatedTopicArn", topic1, t => t.TopicArn, "AirportCreatedTopicArn")
                               .WithEnvironment("AIRPORTS_SNS__AirportUpdatedTopicArn", topic2, t => t.TopicArn, "AirportUpdatedTopicArn")
                               .WithEnvironment("AIRPORTS_DynamoDb__TableName", "airports");
builder.AddAWSAPIGatewayEmulator("api-gateway", APIGatewayType.HttpV2, new APIGatewayEmulatorOptions
{
    HttpPort = 3000
})
.WithReference(airportsApiLambda, Method.Any, "/")
.WithReference(airportsApiLambda, Method.Any, "/{proxy+}");
await builder.Build().RunAsync();
