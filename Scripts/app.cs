#:package AWSSDK.ECR@4.0.3.6
#:package Ductus.FluentDocker@2.85.0
using System.IO.Pipelines;
using Amazon.ECR;
using Amazon.ECR.Model;
using Ductus.FluentDocker.Builders;

await CreateRepositoryAsync("embassy-web");

static async Task CreateRepositoryAsync(string name)
{
    using var client = new AmazonECRClient();
    var req = new DescribeRepositoriesRequest();
    var res = await client.DescribeRepositoriesAsync(req);
    var exists = res.Repositories.Exists(r => r.RepositoryName == name);
    if (!exists)
    {
        Console.WriteLine("Repository does not exist. Creating...");
    }
    var req2 = new CreateRepositoryRequest
    {
        RepositoryName = "embassy-web"
    };
    var res2 = await client.CreateRepositoryAsync(req2);
    Console.WriteLine($"Created repository: {res2.Repository.RepositoryName}");
    Console.WriteLine($"Uri: {res2.Repository.RepositoryUri}");
}