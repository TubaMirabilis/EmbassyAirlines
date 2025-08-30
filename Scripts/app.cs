#:package AWSSDK.ECR@4.0.4.1
#:package AWSSDK.EC2@4.0.35
#:package Ductus.FluentDocker@2.85.0

using Amazon.EC2;
using Amazon.EC2.Model;

using var client = new AmazonEC2Client();
var req = new CreateVpcRequest();
var res = await client.CreateVpcAsync(req);