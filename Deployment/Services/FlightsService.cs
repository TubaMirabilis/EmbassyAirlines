using System.Globalization;
using Amazon.CDK;
using Constructs;
using Deployment.Lambdas;

namespace Deployment.Services;

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
        new HttpDockerLambda(this, "FlightsApi", new HttpDockerLambdaProps
        {
            Api = props.Api,
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Flights.Api.Lambda.dockerfile",
            Environment = commonEnv,
            FunctionName = "FlightsApiLambda",
            RoutePath = "/flights",
            SecurityGroupDescription = "Security group for Flights API Lambda",
            Vpc = props.Vpc
        });
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
        new PublisherLambda(this, "FlightsPublisherLambda", new PublisherLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Flights.Publisher.Lambda.dockerfile",
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
            FunctionName = "FlightsPublisherLambda",
            PollInterval = Duration.Minutes(1),
            SecurityGroupDescription = "Security group for Flights outbox publisher Lambda",
            Topics =
            [
                props.FlightScheduledTopic,
                props.AircraftAssignedToFlightTopic,
                props.FlightPricingAdjustedTopic,
                props.FlightRescheduledTopic,
                props.FlightCancelledTopic,
                props.FlightDelayedTopic,
                props.FlightMarkedAsEnRouteTopic,
                props.FlightMarkedAsDelayedEnRouteTopic,
                props.FlightArrivedTopic
            ],
            Vpc = props.Vpc
        });
    }
}
