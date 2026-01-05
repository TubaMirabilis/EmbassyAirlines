using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SNS;

namespace Deployment;

internal sealed record EventHandlerLambdaProps
{
    internal required int DbPort { get; init; }
    internal required DatabaseProxy DbProxy { get; init; }
    internal required SecurityGroup DbProxySecurityGroup { get; init; }
    internal required string DbUsername { get; init; }
    internal required Dictionary<string, string> Environment { get; init; }
    internal required string FunctionName { get; init; }
    internal required string Handler { get; init; }
    internal required string Path { get; init; }
    internal required string SecurityGroupDescription { get; init; }
    internal required Topic Topic { get; init; }
    internal required Vpc Vpc { get; init; }
}
