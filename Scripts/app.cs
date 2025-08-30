#:package AWSSDK.ECR@4.0.3.6
#:package Ductus.FluentDocker@2.85.0
using System.Diagnostics;
using System.IO.Pipelines;
using Amazon.ECR;
using Amazon.ECR.Model;
using Ductus.FluentDocker.Builders;

var uri = await CreateRepositoryAsync("embassy-web");
await BuildDockerImageAsync();
var localImageTag = "tubamirabilis/example:latest";
var remoteImageUri = $"{uri}:latest";
RunDockerCommand($"tag {localImageTag} {remoteImageUri}");
Console.WriteLine($"Pushing image {remoteImageUri} to ECR...");
RunDockerCommand($"push {remoteImageUri}");
Console.WriteLine("Image push completed.");

static async Task<string> CreateRepositoryAsync(string name)
{
    using var client = new AmazonECRClient();
    var req = new DescribeRepositoriesRequest();
    var res = await client.DescribeRepositoriesAsync(req);
    var exists = res.Repositories.Exists(r => r.RepositoryName == name);
    if (exists)
    {
        Console.WriteLine($"Repository {name} already exists.");
        return res.Repositories.First(r => r.RepositoryName == name).RepositoryUri;
    }
    var req2 = new CreateRepositoryRequest
    {
        RepositoryName = "embassy-web"
    };
    var res2 = await client.CreateRepositoryAsync(req2);
    Console.WriteLine($"Created repository: {res2.Repository.RepositoryName}");
    return res2.Repository.RepositoryUri;
}

static async Task<string> BuildDockerImageAsync()
{
    Console.WriteLine("Building Docker image...");
    var dir = Directory.GetCurrentDirectory();
    var dockerfilePath = Path.Combine(dir, "docker", "Example.Api.Lambda.dockerfile");
    if (!File.Exists(dockerfilePath))
    {
        throw new FileNotFoundException("Dockerfile not found.", dockerfilePath);
    }
    var dockerfileContents = await File.ReadAllTextAsync(dockerfilePath);
    var image = new Builder().DefineImage("tubamirabilis/example")
                            .ReuseIfAlreadyExists()
                            .FromString(dockerfileContents)
                            .WorkingFolder(dir)
                            .Build();
    return image.Id;
}

    static void RunDockerCommand(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (s, e) => { if (e.Data != null) { Console.WriteLine(e.Data); } };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) { Console.Error.WriteLine(e.Data); } };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker command failed: docker {arguments}");
        }
    }