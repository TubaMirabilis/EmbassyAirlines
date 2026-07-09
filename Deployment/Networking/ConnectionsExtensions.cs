using Amazon.CDK.AWS.EC2;

namespace Deployment.Networking;

internal static class ConnectionsExtensions
{
    public static void AllowTo(this Connections_ connections, SecurityGroupConnection rule) =>
        connections.AllowTo(rule.Other, rule.PortRange, rule.Description);
}
