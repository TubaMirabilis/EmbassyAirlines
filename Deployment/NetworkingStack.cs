using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Deployment;

internal sealed class NetworkingStack : Stack
{
    internal NetworkingStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
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
        Vpc.AddInterfaceEndpoint("SnsInterfaceEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.SNS
        });
        Vpc.AddInterfaceEndpoint("SecretsManagerInterfaceEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.SECRETS_MANAGER
        });
        Vpc.AddGatewayEndpoint("DynamoDbEndpoint", new GatewayVpcEndpointOptions
{
    Service = GatewayVpcEndpointAwsService.DYNAMODB,
    Subnets =
    [
        new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED }
    ]
});
    }
    internal Vpc Vpc { get; }
}
