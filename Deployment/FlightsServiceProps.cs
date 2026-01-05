using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SNS;

namespace Deployment;

internal sealed record FlightsServiceProps
{
    internal required Topic AircraftCreatedTopic { get; init; }
    internal required Topic AirportCreatedTopic { get; init; }
    internal required Topic AirportUpdatedTopic { get; init; }
    internal required HttpApi Api { get; init; }
    internal required string DbName { get; init; }
    internal required int DbPort { get; init; }
    internal required DatabaseProxy DbProxy { get; init; }
    internal required SecurityGroup DbProxySecurityGroup { get; init; }
    internal required string DbUsername { get; init; }
    internal required Topic FlightScheduledTopic { get; init; }
    internal required Vpc Vpc { get; init; }
}
