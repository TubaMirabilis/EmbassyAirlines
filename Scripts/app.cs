#:package AWSSDK.ECR@4.0.3.6

using Amazon.ECR;
using Amazon.ECR.Model;

using var client = new AmazonECRClient();
var req = new DescribeRegistryRequest();
var res = await client.DescribeRegistryAsync(req);
Console.WriteLine(res.RegistryId);