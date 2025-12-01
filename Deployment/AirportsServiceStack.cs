using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class AirportsServiceStack : Stack
{
    internal AirportsServiceStack(Construct scope, string id, AirportsServiceStackProps props) : base(scope, id, props)
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
        var imageCode = DockerImageCode.FromImageAsset(directory: ".", new AssetImageCodeProps
        {
            File = "docker/Airports.Api.Lambda.dockerfile"
        });
        var lambdaSg = new SecurityGroup(this, "AirportsLambdaSG", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            AllowAllOutbound = true,
            Description = "Security group for Airports API Lambda"
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
            },
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },
            SecurityGroups = [lambdaSg]
        });
        airportsTable.GrantReadWriteData(lambda);
        airportCreatedTopic.GrantPublish(lambda);
        airportUpdatedTopic.GrantPublish(lambda);
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = "/airports",
            Integration = new HttpLambdaIntegration("AirportsApiIntegration", lambda),
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
    }
}
