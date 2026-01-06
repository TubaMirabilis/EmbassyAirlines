using Amazon.CDK;
using Constructs;

namespace Deployment;

internal sealed class EmbassyAirlinesStack : Stack
{
    internal EmbassyAirlinesStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
        var network = new Network(this, "Networking");
        var messaging = new MessagingResources(this, "Messaging");
        var shared = new SharedInfra(this, "Shared");
        var rds = new RdsResources(this, "RDS", new RdsResourcesProps
        {
            DbName = "embassyairlines",
            DbPort = 5432,
            DbUsername = "embassyadmin",
            Vpc = network.Vpc
        });
        new AirportsService(this, "AirportsService", new AirportsServiceProps
        {
            AirportCreatedTopic = messaging.AirportCreatedTopic,
            AirportUpdatedTopic = messaging.AirportUpdatedTopic,
            Api = shared.Api,
            Vpc = network.Vpc
        });
        new AircraftService(this, "AircraftService", new AircraftServiceProps
        {
            AircraftCreatedTopic = messaging.AircraftCreatedTopic,
            Api = shared.Api,
            DbName = rds.DbName,
            DbPort = rds.DbPort,
            DbProxy = rds.DbProxy,
            DbProxySecurityGroup = rds.DbProxySecurityGroup,
            DbUsername = "embassyadmin",
            Vpc = network.Vpc
        });
        new FlightsService(this, "FlightsService", new FlightsServiceProps
        {
            AircraftCreatedTopic = messaging.AircraftCreatedTopic,
            AirportCreatedTopic = messaging.AirportCreatedTopic,
            AirportUpdatedTopic = messaging.AirportUpdatedTopic,
            Api = shared.Api,
            DbName = rds.DbName,
            DbPort = rds.DbPort,
            DbProxy = rds.DbProxy,
            DbProxySecurityGroup = rds.DbProxySecurityGroup,
            DbUsername = "embassyadmin",
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
