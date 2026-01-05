using System.Globalization;
using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class FlightsService : Construct
{
    internal FlightsService(Construct scope, string id, FlightsServiceProps props) : base(scope, id)
    {
        var commonEnv = new Dictionary<string, string>
            {
                { "FLIGHTS_DbConnection__Database", props.DbName },
                { "FLIGHTS_DbConnection__Host", props.DbProxy.Endpoint },
                { "FLIGHTS_DbConnection__Port", props.DbPort.ToString(CultureInfo.InvariantCulture) },
                { "FLIGHTS_DbConnection__Username", props.DbUsername }
            };
        var imageCode = DockerImageCode.FromImageAsset(directory: ".", new AssetImageCodeProps
        {
            File = "docker/Flights.Api.Lambda.dockerfile"
        });
        var lambdaSg = new SecurityGroup(this, "FlightsLambdaSG", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            AllowAllOutbound = true,
            Description = "Security group for Flights API Lambda"
        });
        var apiLambda = new DockerImageFunction(this, "FlightsApiLambda", new DockerImageFunctionProps
        {
            FunctionName = "FlightsApiLambda",
            Code = imageCode,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>(commonEnv)
            {
                { "FLIGHTS_SNS__FlightScheduledTopicArn", props.FlightScheduledTopic.TopicArn }
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
            Path = "/flights",
            Integration = new HttpLambdaIntegration("FlightsApiIntegration", apiLambda),
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
        props.DbProxy.GrantConnect(apiLambda, props.DbUsername);
        lambdaSg.Connections.AllowTo(props.DbProxySecurityGroup, Port.Tcp(props.DbPort), "Allow Lambda to access RDS Proxy");
        props.FlightScheduledTopic.GrantPublish(apiLambda);
        new EventHandlerLambda(this, "FlightsAircraftCreatedHandlerLambda", new EventHandlerLambdaProps
        {
            DbPort = props.DbPort,
            DbProxy = props.DbProxy,
            DbProxySecurityGroup = props.DbProxySecurityGroup,
            DbUsername = props.DbUsername,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAircraftCreatedHandlerLambda",
            Path = "docker/Flights.Api.Lambda.MessageHandlers.AircraftCreated.dockerfile",
            SecurityGroupDescription = "Security group for Flights AircraftCreated handler Lambda",
            Topic = props.AircraftCreatedTopic,
            Vpc = props.Vpc
        });
        new EventHandlerLambda(this, "FlightsAirportCreatedHandlerLambda", new EventHandlerLambdaProps
        {
            DbPort = props.DbPort,
            DbProxy = props.DbProxy,
            DbProxySecurityGroup = props.DbProxySecurityGroup,
            DbUsername = props.DbUsername,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAirportCreatedHandlerLambda",
            Path = "docker/Flights.Api.Lambda.MessageHandlers.AirportCreated.dockerfile",
            SecurityGroupDescription = "Security group for Flights AirportCreated handler Lambda",
            Topic = props.AirportCreatedTopic,
            Vpc = props.Vpc
        });
        new EventHandlerLambda(this, "FlightsAirportUpdatedHandlerLambda", new EventHandlerLambdaProps
        {
            DbPort = props.DbPort,
            DbProxy = props.DbProxy,
            DbProxySecurityGroup = props.DbProxySecurityGroup,
            DbUsername = props.DbUsername,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAirportUpdatedHandlerLambda",
            Path = "docker/Flights.Api.Lambda.MessageHandlers.AirportUpdated.dockerfile",
            SecurityGroupDescription = "Security group for Flights AirportUpdated handler Lambda",
            Topic = props.AirportUpdatedTopic,
            Vpc = props.Vpc
        });
    }
}
