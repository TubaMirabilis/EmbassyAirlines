using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class AircraftServiceStack : Stack
{
    internal AircraftServiceStack(Construct scope, string id, AircraftServiceStackProps props) : base(scope, id, props)
    {
        const string dbName = "aircraft";
        var environmentName = new CfnParameter(this, "EnvironmentName", new CfnParameterProps
        {
            Type = "String",
            AllowedPattern = "^[a-zA-Z][a-zA-Z0-9-_]{3,}$",
            Description = "The name of the deployment environment (e.g., Development, Staging, Production)."
        });
        var bucketName = new CfnParameter(this, "AircraftBucketName", new CfnParameterProps
        {
            Type = "String",
            Description = "The name of the S3 bucket for aircraft data."
        });
        var connectionString = $"Server={props.DbInstance.DbInstanceEndpointAddress};" + $"Port={props.DbInstance.DbInstanceEndpointPort};" + $"Database={dbName};" + $"User Id={props.DbUser.ValueAsString};" + $"Password={props.DbPasswordParam.ValueAsString};" + $"Include Error Detail=true";
        var connectionStringSecret = new Secret(this, "AircraftConnectionStringSecret", new SecretProps
        {
            SecretName = $"{environmentName.ValueAsString}/Aircraft/ConnectionStrings__DefaultConnection",
            SecretStringValue = SecretValue.UnsafePlainText(connectionString)
        });
        var aircraftCreatedTopic = new Topic(this, "AircraftCreatedTopic", new TopicProps
        {
            TopicName = "AircraftCreatedTopic"
        });
        var bucket = new Bucket(this, "AircraftBucket", new Amazon.CDK.AWS.S3.BucketProps
        {
            BucketName = bucketName.ValueAsString,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });
        var imageCode = DockerImageCode.FromImageAsset(directory: ".", new AssetImageCodeProps
        {
            File = "docker/Aircraft.Api.Lambda.dockerfile"
        });
        var lambdaSg = new SecurityGroup(this, "AircraftLambdaSG", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            AllowAllOutbound = false,
            Description = "Security group for Aircraft API Lambda"
        });
        var lambda = new DockerImageFunction(this, "AircraftApiLambda", new DockerImageFunctionProps
        {
            FunctionName = "AircraftApiLambda",
            Code = imageCode,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                { "AIRCRAFT_S3__BucketName", bucket.BucketName },
                { "AIRCRAFT_SNS__AircraftCreatedTopicArn", aircraftCreatedTopic.TopicArn }
            },
            Vpc = props.Vpc,
            SecurityGroups = [lambdaSg]
        });
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = "/aircraft",
            Integration = new HttpLambdaIntegration("AircraftApiIntegration", lambda),
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
        aircraftCreatedTopic.GrantPublish(lambda);
        bucket.GrantRead(lambda);
        connectionStringSecret.GrantRead(lambda);
        lambda.Connections.AllowTo(props.DbInstance, Port.Tcp(5432), "Allow Lambda to access RDS");
    }
}
