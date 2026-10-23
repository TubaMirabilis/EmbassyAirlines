using System.Globalization;
using Amazon.CDK;
using Constructs;
using Deployment.Lambdas;

namespace Deployment.Services;

internal sealed class FlightsService : Construct
{
    internal FlightsService(Construct scope, string id, ServiceProps props) : base(scope, id)
    {
        var commonEnv = new Dictionary<string, string>
        {
            { "FLIGHTS_DbConnection__Database", props.DbConnection.DbName },
            { "FLIGHTS_DbConnection__Host", props.DbProxyAccess.DbProxy.Endpoint },
            { "FLIGHTS_DbConnection__Port", props.DbConnection.DbPort.ToString(CultureInfo.InvariantCulture) },
            { "FLIGHTS_DbConnection__Username", props.DbConnection.DbUsername }
        };
        var migrations = new DatabaseMigrationLambda(this, "FlightsMigrations", new DatabaseBackedLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Flights.Migrations.Lambda.dockerfile",
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsMigrationsLambda",
            SecurityGroupDescription = "Security group for Flights migrations Lambda",
            Vpc = props.Vpc
        });
        var api = new HttpDockerLambda(this, "FlightsApi", new HttpDockerLambdaProps
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
        api.Function.Node.AddDependency(migrations.Migration);
        var handler1 = new EventHandlerLambda(this, "FlightsAircraftCreatedHandlerLambda", new EventHandlerLambdaProps
        {
            AlarmTopic = props.Topics["Alarm"],
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Flights.Api.Lambda.MessageHandlers.AircraftCreated.dockerfile",
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAircraftCreatedHandlerLambda",
            SecurityGroupDescription = "Security group for Flights AircraftCreated handler Lambda",
            Topic = props.Topics["AircraftCreated"],
            Vpc = props.Vpc
        });
        handler1.Function.Node.AddDependency(migrations.Migration);
        var handler2 = new EventHandlerLambda(this, "FlightsAirportCreatedHandlerLambda", new EventHandlerLambdaProps
        {
            AlarmTopic = props.Topics["Alarm"],
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Flights.Api.Lambda.MessageHandlers.AirportCreated.dockerfile",
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAirportCreatedHandlerLambda",
            SecurityGroupDescription = "Security group for Flights AirportCreated handler Lambda",
            Topic = props.Topics["AirportCreated"],
            Vpc = props.Vpc
        });
        handler2.Function.Node.AddDependency(migrations.Migration);
        var handler3 = new EventHandlerLambda(this, "FlightsAirportUpdatedHandlerLambda", new EventHandlerLambdaProps
        {
            AlarmTopic = props.Topics["Alarm"],
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Flights.Api.Lambda.MessageHandlers.AirportUpdated.dockerfile",
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "FlightsAirportUpdatedHandlerLambda",
            SecurityGroupDescription = "Security group for Flights AirportUpdated handler Lambda",
            Topic = props.Topics["AirportUpdated"],
            Vpc = props.Vpc
        });
        handler3.Function.Node.AddDependency(migrations.Migration);
        var publisher = new PublisherLambda(this, "FlightsPublisherLambda", new PublisherLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Flights.Publisher.Lambda.dockerfile",
            Environment = new Dictionary<string, string>(commonEnv)
            {
                { "FLIGHTS_SNS__FlightScheduledTopicArn", props.Topics["FlightScheduled"].TopicArn },
                { "FLIGHTS_SNS__AircraftAssignedToFlightTopicArn", props.Topics["AircraftAssignedToFlight"].TopicArn },
                { "FLIGHTS_SNS__FlightPricingAdjustedTopicArn", props.Topics["FlightPricingAdjusted"].TopicArn },
                { "FLIGHTS_SNS__FlightRescheduledTopicArn", props.Topics["FlightRescheduled"].TopicArn },
                { "FLIGHTS_SNS__FlightCancelledTopicArn", props.Topics["FlightCancelled"].TopicArn },
                { "FLIGHTS_SNS__FlightDelayedTopicArn", props.Topics["FlightDelayed"].TopicArn },
                { "FLIGHTS_SNS__FlightMarkedAsEnRouteTopicArn", props.Topics["FlightMarkedAsEnRoute"].TopicArn },
                { "FLIGHTS_SNS__FlightMarkedAsDelayedEnRouteTopicArn", props.Topics["FlightMarkedAsDelayedEnRoute"].TopicArn },
                { "FLIGHTS_SNS__FlightArrivedTopicArn", props.Topics["FlightArrived"].TopicArn }
            },
            FunctionName = "FlightsPublisherLambda",
            PollInterval = Duration.Minutes(1),
            SecurityGroupDescription = "Security group for Flights outbox publisher Lambda",
            Topics =
            [
                props.Topics["FlightScheduled"],
                props.Topics["AircraftAssignedToFlight"],
                props.Topics["FlightPricingAdjusted"],
                props.Topics["FlightRescheduled"],
                props.Topics["FlightCancelled"],
                props.Topics["FlightDelayed"],
                props.Topics["FlightMarkedAsEnRoute"],
                props.Topics["FlightMarkedAsDelayedEnRoute"],
                props.Topics["FlightArrived"]
            ],
            Vpc = props.Vpc
        });
        publisher.Function.Node.AddDependency(migrations.Migration);
    }
}
