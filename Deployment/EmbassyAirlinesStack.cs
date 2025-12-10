using Amazon.CDK;
using Constructs;

namespace Deployment;

internal sealed class EmbassyAirlinesStack : Stack
{
    internal EmbassyAirlinesStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
        var network = new Network(this, "Networking");
        var shared = new SharedInfra(this, "Shared");
        var rds = new RdsResources(this, "RDS", new RdsResourcesProps()
        {
            Vpc = network.Vpc
        });
        new AirportsService(this, "AirportsService", new AirportsServiceProps()
        {
            Api = shared.Api,
            Vpc = network.Vpc
        });
        new AircraftService(this, "AircraftService", new AircraftServiceProps()
        {
            Api = shared.Api,
            DbProxy = rds.DbProxy,
            DbProxySecurityGroup = rds.DbProxySecurityGroup,
            Vpc = network.Vpc
        });
    }
}
