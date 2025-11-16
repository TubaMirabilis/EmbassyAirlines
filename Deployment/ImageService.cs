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
        await DockerCommandRunner.RunCommandAsync($"tag {localImageTag} {remoteImageUri}");
    }
    public static async Task RunAsync(string imageTag, int hostPort, int containerPort, string name, Dictionary<string, string> env)
    {
        Console.WriteLine($"Running image {imageTag} on port {hostPort}:{containerPort}");
        if (env.Count == 0)
        {
            await DockerCommandRunner.RunCommandAsync($"run -d -p {hostPort}:{containerPort} --name {name} {imageTag}");
            return;
        }
        var envArgs = string.Join(" ", env.Select(kv => $"-e {kv.Key}={kv.Value}"));
        await DockerCommandRunner.RunCommandAsync($"run -d -p {hostPort}:{containerPort} {envArgs} --name {name} {imageTag}");
    }
    public static async Task StopAndRemoveContainerAsync(string name)
    {
        Console.WriteLine($"Stopping and removing container {name}");
        await DockerCommandRunner.RunCommandAsync($"rm -f {name}");
    }
    public static async Task PushAsync(string imageTag)
    {
        Console.WriteLine($"Pushing image {imageTag}");
        await DockerCommandRunner.RunCommandAsync($"push {imageTag}");
    }
}
