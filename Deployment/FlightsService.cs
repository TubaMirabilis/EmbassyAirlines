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
            { "FLIGHTS_DbConnection__Database", props.DbConnection.DbName },
            { "FLIGHTS_DbConnection__Host", props.DbProxyAccess.DbProxy.Endpoint },
            { "FLIGHTS_DbConnection__Port", props.DbConnection.DbPort.ToString(CultureInfo.InvariantCulture) },
            { "FLIGHTS_DbConnection__Username", props.DbConnection.DbUsername }
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
                { "FLIGHTS_SNS__FlightScheduledTopicArn", props.FlightScheduledTopic.TopicArn },
                { "FLIGHTS_SNS__AircraftAssignedToFlightTopicArn", props.AircraftAssignedToFlightTopic.TopicArn },
                { "FLIGHTS_SNS__FlightPricingAdjustedTopicArn", props.FlightPricingAdjustedTopic.TopicArn },
                { "FLIGHTS_SNS__FlightRescheduledTopicArn", props.FlightRescheduledTopic.TopicArn },
                { "FLIGHTS_SNS__FlightCancelledTopicArn", props.FlightCancelledTopic.TopicArn },
                { "FLIGHTS_SNS__FlightDelayedTopicArn", props.FlightDelayedTopic.TopicArn },
                { "FLIGHTS_SNS__FlightMarkedAsEnRouteTopicArn", props.FlightMarkedAsEnRouteTopic.TopicArn },
                { "FLIGHTS_SNS__FlightMarkedAsDelayedEnRouteTopicArn", props.FlightMarkedAsDelayedEnRouteTopic.TopicArn },
                { "FLIGHTS_SNS__FlightArrivedTopicArn", props.FlightArrivedTopic.TopicArn }
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
        props.DbProxyAccess.DbProxy.GrantConnect(apiLambda, props.DbConnection.DbUsername);
        lambdaSg.Connections.AllowTo(props.DbProxyAccess.DbProxySecurityGroup, Port.Tcp(props.DbConnection.DbPort), "Allow Lambda to access RDS Proxy");
        props.FlightScheduledTopic.GrantPublish(apiLambda);
        props.AircraftAssignedToFlightTopic.GrantPublish(apiLambda);
        props.FlightPricingAdjustedTopic.GrantPublish(apiLambda);
        props.FlightRescheduledTopic.GrantPublish(apiLambda);
        props.FlightCancelledTopic.GrantPublish(apiLambda);
        props.FlightDelayedTopic.GrantPublish(apiLambda);
        props.FlightMarkedAsEnRouteTopic.GrantPublish(apiLambda);
        props.FlightMarkedAsDelayedEnRouteTopic.GrantPublish(apiLambda);
        props.FlightArrivedTopic.GrantPublish(apiLambda);
        new EventHandlerLambda(this, "FlightsAircraftCreatedHandlerLambda", new EventHandlerLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAircraftCreatedHandlerLambda",
            Path = "docker/Flights.Api.Lambda.MessageHandlers.AircraftCreated.dockerfile",
            SecurityGroupDescription = "Security group for Flights AircraftCreated handler Lambda",
            Topic = props.AircraftCreatedTopic,
            Vpc = props.Vpc
        });
        new EventHandlerLambda(this, "FlightsAirportCreatedHandlerLambda", new EventHandlerLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAirportCreatedHandlerLambda",
            Path = "docker/Flights.Api.Lambda.MessageHandlers.AirportCreated.dockerfile",
            SecurityGroupDescription = "Security group for Flights AirportCreated handler Lambda",
            Topic = props.AirportCreatedTopic,
            Vpc = props.Vpc
        });
        new EventHandlerLambda(this, "FlightsAirportUpdatedHandlerLambda", new EventHandlerLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAirportUpdatedHandlerLambda",
            Path = "docker/Flights.Api.Lambda.MessageHandlers.AirportUpdated.dockerfile",
            SecurityGroupDescription = "Security group for Flights AirportUpdated handler Lambda",
            Topic = props.AirportUpdatedTopic,
            Vpc = props.Vpc
        });
    }
}
