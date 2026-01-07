using System.Globalization;
using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class AircraftService : Construct
{
    internal AircraftService(Construct scope, string id, AircraftServiceProps props) : base(scope, id)
    {
        var commonEnv = new Dictionary<string, string>
        {
            { "AIRCRAFT_DbConnection__Database", props.DbName },
                { "AIRCRAFT_DbConnection__Host", props.DbProxy.Endpoint },
                { "AIRCRAFT_DbConnection__Port", props.DbPort.ToString(CultureInfo.InvariantCulture) },
                { "AIRCRAFT_DbConnection__Username", props.DbUsername }
        };
        var bucket = new Bucket(this, "AircraftBucket", new BucketProps
        {
            BucketName = $"aircraft-bucket-{Aws.ACCOUNT_ID}-{Aws.REGION}",
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
            Environment = new Dictionary<string, string>(commonEnv)
            {
                { "AIRCRAFT_S3__BucketName", bucket.BucketName },
                { "AIRCRAFT_SNS__AircraftCreatedTopicArn", props.AircraftCreatedTopic.TopicArn }
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
        props.DbProxy.GrantConnect(lambda, props.DbUsername);
        lambdaSg.Connections.AllowTo(props.DbProxySecurityGroup, Port.Tcp(props.DbPort), "Allow Lambda to access RDS Proxy");
        props.AircraftCreatedTopic.GrantPublish(lambda);
        bucket.GrantRead(lambda);
        new EventHandlerLambda(this, "AircraftFlightArrivedHandlerLambda", new EventHandlerLambdaProps
        {
            DbPort = props.DbPort,
            DbProxy = props.DbProxy,
            DbProxySecurityGroup = props.DbProxySecurityGroup,
            DbUsername = props.DbUsername,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "AircraftFlightArrivedHandlerLambda",
            Path = "docker/Aircraft.Api.Lambda.MessageHandlers.FlightArrived.dockerfile",
            SecurityGroupDescription = "Security group for Aircraft FlightArrived handler Lambda",
            Topic = props.FlightArrivedTopic,
            Vpc = props.Vpc
        });
        new EventHandlerLambda(this, "AircraftFlightMarkedAsDelayedEnRouteHandlerLambda", new EventHandlerLambdaProps
        {
            DbPort = props.DbPort,
            DbProxy = props.DbProxy,
            DbProxySecurityGroup = props.DbProxySecurityGroup,
            DbUsername = props.DbUsername,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "AircraftFlightMarkedAsDelayedEnRouteHandlerLambda",
            Path = "docker/Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute.dockerfile",
            SecurityGroupDescription = "Security group for Aircraft FlightMarkedAsDelayedEnRoute handler Lambda",
            Topic = props.FlightMarkedAsDelayedEnRouteTopic,
            Vpc = props.Vpc
        });
        new EventHandlerLambda(this, "AircraftFlightMarkedAsEnRouteHandlerLambda", new EventHandlerLambdaProps
        {
            DbPort = props.DbPort,
            DbProxy = props.DbProxy,
            DbProxySecurityGroup = props.DbProxySecurityGroup,
            DbUsername = props.DbUsername,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "AircraftFlightMarkedAsEnRouteHandlerLambda",
            Path = "docker/Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsEnRoute.dockerfile",
            SecurityGroupDescription = "Security group for Aircraft FlightMarkedAsEnRoute handler Lambda",
            Topic = props.FlightMarkedAsEnRouteTopic,
            Vpc = props.Vpc
        });
    }
}
