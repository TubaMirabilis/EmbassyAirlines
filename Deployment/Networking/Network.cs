using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Deployment.Networking;

internal sealed class Network : Construct
{
    internal Vpc Vpc { get; }
    internal SecurityGroup VpcEndpointSecurityGroup { get; }

    internal Network(Construct scope, string id)
        : base(scope, id)
    {
        Vpc = new Vpc(this, "EmbassyAirlinesVpc", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = 0
        });

        VpcEndpointSecurityGroup = new SecurityGroup(
            this,
            "VpcEndpointSecurityGroup",
            new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Security group for AWS interface VPC endpoints",
                AllowAllOutbound = false
            });

        Vpc.AddGatewayEndpoint(
            "S3GatewayEndpoint",
            new GatewayVpcEndpointOptions
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

        var endpointSubnets = new SubnetSelection
        {
            SubnetType = SubnetType.PRIVATE_ISOLATED
        };

        Vpc.AddInterfaceEndpoint(
            "SqsInterfaceEndpoint",
            new InterfaceVpcEndpointOptions
            {
                Service = InterfaceVpcEndpointAwsService.SQS,
                PrivateDnsEnabled = true,
                Subnets = endpointSubnets,
                SecurityGroups = [VpcEndpointSecurityGroup]
            });

        Vpc.AddInterfaceEndpoint(
            "SnsInterfaceEndpoint",
            new InterfaceVpcEndpointOptions
            {
                Service = InterfaceVpcEndpointAwsService.SNS,
                PrivateDnsEnabled = true,
                Subnets = endpointSubnets,
                SecurityGroups = [VpcEndpointSecurityGroup]
            });

        Vpc.AddInterfaceEndpoint(
            "XRayInterfaceEndpoint",
            new InterfaceVpcEndpointOptions
            {
                Service = InterfaceVpcEndpointAwsService.XRAY,
                PrivateDnsEnabled = true,
                Subnets = endpointSubnets,
                SecurityGroups = [VpcEndpointSecurityGroup]
            });
    }
}
