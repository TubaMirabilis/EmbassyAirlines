using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SNS;
using Constructs;

namespace Deployment;

internal sealed class EmbassyAirlinesStack : Stack
{
    internal EmbassyAirlinesStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
        var airportsTable = new Table(this, "AirportsTable", new TableProps
        {
            TableName = "airports",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "Id",
                Type = AttributeType.STRING
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
        var airportCreatedTopic = new Topic(this, "AirportCreatedTopic", new TopicProps
        {
            TopicName = "AirportCreatedTopic"
        });
        var airportUpdatedTopic = new Topic(this, "AirportUpdatedTopic", new TopicProps
        {
            TopicName = "AirportUpdatedTopic"
        });
        var imageCode = DockerImageCode.FromImageAsset(directory: "../docker", new AssetImageCodeProps
        {
            File = "Airports.Api.Lambda.dockerfile"
        });
        var lambda = new DockerImageFunction(this, "AirportsApiLambda", new DockerImageFunctionProps
        {
            FunctionName = "AirportsApiLambda",
            Code = imageCode,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                { "AIRPORTS_DynamoDb__TableName", airportsTable.TableName },
                { "AIRPORTS_SNS__AirportCreatedTopicArn", airportCreatedTopic.TopicArn },
                { "AIRPORTS_SNS__AirportUpdatedTopicArn", airportUpdatedTopic.TopicArn }
            }
        });
        airportsTable.GrantReadWriteData(lambda);
        airportCreatedTopic.GrantPublish(lambda);
        airportUpdatedTopic.GrantPublish(lambda);
        var functionUrl = lambda.AddFunctionUrl(new FunctionUrlOptions
        {
            AuthType = FunctionUrlAuthType.NONE
        });
        new CfnOutput(this, "AirportsFunctionUrl", new CfnOutputProps
        {
            Value = functionUrl.Url,
            Description = "Public URL for AirportsApiLambda"
        });
    }
}
