using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class AirportsService : Construct
{
    internal AirportsService(Construct scope, string id, AirportsServiceProps props) : base(scope, id)
    {
        var airportsTable = new Table(this, "AirportsTable", new TableProps
        {
            TableName = "airports",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "Id",
                Type = AttributeType.STRING
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.DESTROY
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
                { "AIRPORTS_SNS__AirportCreatedTopicArn", props.AirportCreatedTopic.TopicArn },
                { "AIRPORTS_SNS__AirportUpdatedTopicArn", props.AirportUpdatedTopic.TopicArn }
            },
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },
            SecurityGroups = [lambdaSg]
        });
        airportsTable.GrantReadWriteData(lambda);
        props.AirportCreatedTopic.GrantPublish(lambda);
        props.AirportUpdatedTopic.GrantPublish(lambda);
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = "/airports",
            Integration = new HttpLambdaIntegration("AirportsApiIntegration", lambda),
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
    }
}
