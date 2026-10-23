using Amazon.CDK;
using Amazon.CDK.AWS.SNS;
using Constructs;
using Deployment.Database;
using Deployment.Networking;
using Deployment.Services;

namespace Deployment;

internal sealed class EmbassyAirlinesStack : Stack
{
    internal EmbassyAirlinesStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
        var network = new Network(this, "Networking");
        var messaging = new MessagingResources(this, "Messaging");
        var shared = new SharedInfra(this, "Shared");
        var dbConnection = new DatabaseConnectionProps
        {
            DbName = "embassyairlines",
            DbPort = 5432,
            DbUsername = "embassyadmin"
        };
        var rds = new RdsResources(this, "RDS", new RdsResourcesProps
        {
            DatabaseConnection = dbConnection,
            Vpc = network.Vpc
        });
        var dbProxyAccess = new DatabaseProxyAccessProps
        {
            DbProxy = rds.DbProxy,
            DbProxySecurityGroup = rds.DbProxySecurityGroup
        };
        new AirportsService(this, "AirportsService", new ServiceProps
        {
            Api = shared.Api,
            DbConnection = dbConnection,
            DbProxyAccess = dbProxyAccess,
            Topics = new Dictionary<string, Topic>
            {
                { "AirportCreated", messaging.AirportCreatedTopic },
                { "AirportUpdated", messaging.AirportUpdatedTopic }
            },
            Vpc = network.Vpc
        });
        new AircraftService(this, "AircraftService", new ServiceProps
        {
            Api = shared.Api,
            DbConnection = dbConnection,
            DbProxyAccess = dbProxyAccess,
            Topics = new Dictionary<string, Topic>
            {
                { "AircraftCreated", messaging.AircraftCreatedTopic },
                { "FlightArrived", messaging.FlightArrivedTopic },
                { "FlightMarkedAsDelayedEnRoute", messaging.FlightMarkedAsDelayedEnRouteTopic },
                { "FlightMarkedAsEnRoute", messaging.FlightMarkedAsEnRouteTopic }
            },
            Vpc = network.Vpc
        });
        new FlightsService(this, "FlightsService", new ServiceProps
        {
            Api = shared.Api,
            DbConnection = dbConnection,
            DbProxyAccess = dbProxyAccess,
            Topics = new Dictionary<string, Topic>
            {
                { "AircraftCreated", messaging.AircraftCreatedTopic },
                { "AirportCreated", messaging.AirportCreatedTopic },
                { "AirportUpdated", messaging.AirportUpdatedTopic },
                { "FlightScheduled", messaging.FlightScheduledTopic },
                { "AircraftAssignedToFlight", messaging.AircraftAssignedToFlightTopic },
                { "FlightPricingAdjusted", messaging.FlightPricingAdjustedTopic },
                { "FlightRescheduled", messaging.FlightRescheduledTopic },
                { "FlightCancelled", messaging.FlightCancelledTopic },
                { "FlightDelayed", messaging.FlightDelayedTopic },
                { "FlightMarkedAsEnRoute", messaging.FlightMarkedAsEnRouteTopic },
                { "FlightMarkedAsDelayedEnRoute", messaging.FlightMarkedAsDelayedEnRouteTopic },
                { "FlightArrived", messaging.FlightArrivedTopic }
            },
            Vpc = network.Vpc,
        });
    }
}
