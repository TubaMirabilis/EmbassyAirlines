using Amazon.CDK;
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
        new AirportsService(this, "AirportsService", new AirportsServiceProps
        {
            AirportCreatedTopic = messaging.AirportCreatedTopic,
            AirportUpdatedTopic = messaging.AirportUpdatedTopic,
            Api = shared.Api,
            DbConnection = dbConnection,
            DbProxyAccess = dbProxyAccess,
            Vpc = network.Vpc
        });
        new AircraftService(this, "AircraftService", new AircraftServiceProps
        {
            AircraftCreatedTopic = messaging.AircraftCreatedTopic,
            Api = shared.Api,
            DbConnection = dbConnection,
            DbProxyAccess = dbProxyAccess,
            FlightArrivedTopic = messaging.FlightArrivedTopic,
            FlightMarkedAsDelayedEnRouteTopic = messaging.FlightMarkedAsDelayedEnRouteTopic,
            FlightMarkedAsEnRouteTopic = messaging.FlightMarkedAsEnRouteTopic,
            Vpc = network.Vpc
        });
        new FlightsService(this, "FlightsService", new FlightsServiceProps
        {
            AircraftCreatedTopic = messaging.AircraftCreatedTopic,
            AirportCreatedTopic = messaging.AirportCreatedTopic,
            AirportUpdatedTopic = messaging.AirportUpdatedTopic,
            Api = shared.Api,
            DbConnection = dbConnection,
            DbProxyAccess = dbProxyAccess,
            FlightScheduledTopic = messaging.FlightScheduledTopic,
            AircraftAssignedToFlightTopic = messaging.AircraftAssignedToFlightTopic,
            FlightPricingAdjustedTopic = messaging.FlightPricingAdjustedTopic,
            FlightRescheduledTopic = messaging.FlightRescheduledTopic,
            FlightCancelledTopic = messaging.FlightCancelledTopic,
            FlightDelayedTopic = messaging.FlightDelayedTopic,
            FlightMarkedAsEnRouteTopic = messaging.FlightMarkedAsEnRouteTopic,
            FlightMarkedAsDelayedEnRouteTopic = messaging.FlightMarkedAsDelayedEnRouteTopic,
            FlightArrivedTopic = messaging.FlightArrivedTopic,
            Vpc = network.Vpc,
        });
    }
}
