using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Deployment;

internal sealed class NetworkingStack : Stack
{
    internal NetworkingStack(Construct scope, string id, IStackProps props) : base(scope, id, props) => Vpc = new Vpc(this, "EmbassyAirlinesVpc", new VpcProps
    {
        MaxAzs = 2,
        NatGateways = 1
    });
    internal Vpc Vpc { get; }
}
