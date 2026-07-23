using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Deployment.Networking;

internal sealed class Network : Construct
{
    internal Network(Construct scope, string id) : base(scope, id)
    {
        Vpc = new Vpc(this, "EmbassyAirlinesVpc", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = 0
        });
        Vpc.AddGatewayEndpoint("S3GatewayEndpoint", new GatewayVpcEndpointOptions
        {
            Service = GatewayVpcEndpointAwsService.S3,
            Subnets =
            [
                new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_ISOLATED
                }
            ]
        });
        Vpc.AddInterfaceEndpoint("SqsInterfaceEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.SQS
        });
        Vpc.AddInterfaceEndpoint("SnsInterfaceEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.SNS
        });
    }
    internal Vpc Vpc { get; }
}
