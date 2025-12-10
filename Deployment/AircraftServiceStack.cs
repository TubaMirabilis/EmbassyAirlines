using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class AircraftServiceStack : Stack
{
    internal AircraftServiceStack(Construct scope, string id, AircraftServiceStackProps props) : base(scope, id, props)
    {
        var bucketName = new CfnParameter(this, "AircraftBucketName", new CfnParameterProps
        {
            Type = "String",
            Description = "The name of the S3 bucket for aircraft data."
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
            AllowAllOutbound = true,
            Description = "Security group for Aircraft API Lambda"
        });
        var lambda = new DockerImageFunction(this, "AircraftApiLambda", new DockerImageFunctionProps
        {
            FunctionName = "AircraftApiLambda",
            Code = imageCode,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                { "AIRCRAFT_DbConnection__Host", props.DbProxy.Endpoint },
                { "AIRCRAFT_DbConnection__Database", "aircraft" },
                { "AIRCRAFT_DbConnection__Username", "embassyadmin" },
                { "AIRCRAFT_S3__BucketName", bucket.BucketName },
                { "AIRCRAFT_SNS__AircraftCreatedTopicArn", aircraftCreatedTopic.TopicArn }
            },
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },
            SecurityGroups = [lambdaSg]
        });
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = "/aircraft",
            Integration = new HttpLambdaIntegration("AircraftApiIntegration", lambda),
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
        props.DbProxy.GrantConnect(lambda, "embassyadmin");
        lambdaSg.Connections.AllowTo(props.DbProxySecurityGroup, Port.Tcp(5432), "Allow Lambda to access RDS Proxy");
        aircraftCreatedTopic.GrantPublish(lambda);
        bucket.GrantRead(lambda);
    }
}
