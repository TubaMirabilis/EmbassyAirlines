using System.Diagnostics;
using Amazon.ECR;
using Amazon.ECR.Model;
using Ductus.FluentDocker.Builders;

namespace Deployment;

internal static class ImageService
{
    public static async Task<string> EnsureRepositoryAsync(string name)
    {
        Console.WriteLine($"Ensuring repository exists: {name}");
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
            RepositoryName = name
        };
        var res2 = await client.CreateRepositoryAsync(req2);
        return res2.Repository.RepositoryUri;
    }
    public static async Task BuildAsync(string fileName, string imageName, string tag)
    {
        Console.WriteLine($"Building image {imageName}:{tag} from {fileName}");
        var dir = Directory.GetCurrentDirectory();
        var dockerfilePath = Path.Combine(dir, "docker", fileName);
        if (!File.Exists(dockerfilePath))
        {
            throw new FileNotFoundException("Dockerfile not found.", dockerfilePath);
        }
        var dockerfileContents = await File.ReadAllTextAsync(dockerfilePath);
        var builder = new Builder();
        builder.DefineImage(imageName)
               .ImageTag(tag)
               .ReuseIfAlreadyExists()
               .FromString(dockerfileContents)
               .WorkingFolder(dir)
               .Build();
        var dockerfile = Path.Combine(dir, "Dockerfile");
        if (File.Exists(dockerfile))
        {
            Console.WriteLine($"Removing temporary Dockerfile: {dockerfile}");
            File.Delete(dockerfile);
        }
    }
    public static async Task TagAsync(string localImageTag, string remoteImageUri)
    {
        Console.WriteLine($"Tagging image {localImageTag} as {remoteImageUri}");
        await RunDockerCommandAsync($"tag {localImageTag} {remoteImageUri}");
    }
    public static async Task PushAsync(string imageTag)
    {
        Console.WriteLine($"Pushing image {imageTag}");
        await RunDockerCommandAsync($"push {imageTag}");
    }
    private static async Task RunDockerCommandAsync(string arguments)
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
        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data is not null)
            {
                Console.WriteLine(e.Data);
            }
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data is not null)
            {
                Console.Error.WriteLine(e.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker command failed: docker {arguments}");
        }
    }
}
