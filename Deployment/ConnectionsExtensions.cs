using Amazon.CDK.AWS.EC2;

namespace Deployment;

internal static class ConnectionsExtensions
{
    public static void AllowTo(this Connections_ connections, ConnectionRule rule) =>
        connections.AllowTo(rule.Other, rule.PortRange, rule.Description);
}
