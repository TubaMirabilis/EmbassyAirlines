using Amazon;
using Amazon.CDK.AWS.DynamoDB;
using Aspire.Hosting.AWS.Lambda;

var builder = DistributedApplication.CreateBuilder(args);
var awsSdkConfig = builder.AddAWSSDKConfig().WithRegion(RegionEndpoint.EUWest2);
var awsResources = builder.AddAWSCDKStack("AirportsServiceAWSResources").WithReference(awsSdkConfig);
builder.AddAWSDynamoDBLocal("DynamoDBAirports").WithReference(awsSdkConfig);
var airportsTable = awsResources.AddDynamoDBTable("AirportsTable", new TableProps
{
    TableName = "airports",
    PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
    {
        Name = "Id",
        Type = AttributeType.STRING
    }
});
var topic1 = awsResources.AddSNSTopic("AirportCreatedTopic");
var topic2 = awsResources.AddSNSTopic("AirportUpdatedTopic");
builder.AddAWSLambdaServiceEmulator();
var airportsApiLambda = builder.AddAWSLambdaFunction<Projects.Airports_Api_Lambda>("airports-api", "Airports.Api.Lambda").WithReference(awsSdkConfig).WithReference(topic1).WithReference(topic2).WithReference(airportsTable).WithEnvironment("AIRPORTS_DynamoDb__TableName", "airports");
builder.AddAWSAPIGatewayEmulator("api-gateway", APIGatewayType.HttpV2, new APIGatewayEmulatorOptions
{
    Port = 3000
})
.WithReference(airportsApiLambda, Method.Any, "/")
.WithReference(airportsApiLambda, Method.Any, "/{proxy+}");
await builder.Build().RunAsync();
