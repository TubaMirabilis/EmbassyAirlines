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
        var rds = new RdsResources(this, "RDS", new RdsResourcesProps()
        {
            Vpc = network.Vpc,
            DbUsername = "embassyadmin"
        });
        new AirportsService(this, "AirportsService", new AirportsServiceProps()
        {
            AirportCreatedTopic = messaging.AirportCreatedTopic,
            AirportUpdatedTopic = messaging.AirportUpdatedTopic,
            Api = shared.Api,
            Vpc = network.Vpc
        });
        new AircraftService(this, "AircraftService", new AircraftServiceProps()
        {
            AircraftCreatedTopic = messaging.AircraftCreatedTopic,
            Api = shared.Api,
            DbProxy = rds.DbProxy,
            DbProxySecurityGroup = rds.DbProxySecurityGroup,
            DbUsername = "embassyadmin",
            Vpc = network.Vpc
        });
        new FlightsService(this, "FlightsService", new FlightsServiceProps()
        {
            FlightScheduledTopic = messaging.FlightScheduledTopic,
            Api = shared.Api,
            DbProxy = rds.DbProxy,
            DbProxySecurityGroup = rds.DbProxySecurityGroup,
            DbUsername = "embassyadmin",
            Vpc = network.Vpc,
            ProcessingQueue = messaging.ProcessingQueue
        });
    }
}
