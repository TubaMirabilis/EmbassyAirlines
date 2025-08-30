#:package AWSSDK.ECR@4.0.3.6
#:package Ductus.FluentDocker@2.85.0
using Amazon.ECR;
using Amazon.ECR.Model;
using Ductus.FluentDocker.Builders;

using var client = new AmazonECRClient();
var req = new DescribeRegistryRequest();
var res = await client.DescribeRegistryAsync(req);
Console.WriteLine($"Registry ID: {res.RegistryId}");
Console.WriteLine("Building Docker image...");
var dir = Directory.GetCurrentDirectory();
var dockerfilePath = Path.Combine(dir, "docker", "Example.Api.Lambda.dockerfile");
if (!File.Exists(dockerfilePath))
{
    Console.WriteLine($"Dockerfile not found at path: {dockerfilePath}");
    return;
}
var dockerfileContents = await File.ReadAllTextAsync(dockerfilePath);
var image = new Builder().DefineImage("tubamirabilis/example")
                         .ReuseIfAlreadyExists()
                         .FromString(dockerfileContents)
                         .WorkingFolder(dir)
                         .Build();
Console.WriteLine($"Image ID: {image.Id}");
