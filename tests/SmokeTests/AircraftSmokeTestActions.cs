using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using CliWrap;
using CliWrap.Buffered;
using Shared.Contracts;

namespace SmokeTests;

internal static class AircraftSmokeTestActions
{
    public static async Task<AircraftDto> AttemptPostAsync(HttpClient client, CreateAircraftDto req)
    {
        Console.WriteLine($"Attempting to create aircraft with tail number {req.TailNumber}");
        var res = await client.PostAsJsonAsync("aircraft", req);
        res.EnsureSuccessStatusCode();
        var stream = await res.Content.ReadAsStreamAsync();
        var aircraft = await JsonSerializer.DeserializeAsync<AircraftDto>(stream, JsonSerializerOptions.Web);
        if (aircraft is null)
        {
            throw new JsonException("Deserialization returned null");
        }
        Console.WriteLine($"Created aircraft with ID: {aircraft.Id}");
        return aircraft;
    }
    public static async Task PrepareS3Async(string path, string key)
    {
        var jsonString = await File.ReadAllTextAsync(path);
        var accountIdQuery = "sts get-caller-identity --query Account --output text";
        var accountQueryArgs = accountIdQuery.Split(' ');
        var accountQueryResult = await Cli.Wrap("aws")
                                          .WithArguments(accountQueryArgs)
                                          .ExecuteBufferedAsync();
        var accountId = accountQueryResult.StandardOutput.Trim();
        var regionQuery = "configure get region";
        var regionQueryArgs = regionQuery.Split(' ');
        var regionQueryResult = await Cli.Wrap("aws")
                                         .WithArguments(regionQueryArgs)
                                         .ExecuteBufferedAsync();
        var region = regionQueryResult.StandardOutput.Trim();
        Console.WriteLine($"Executing AWS S3 upload of file {path} to bucket aircraft-bucket-{accountId}-{region}");
        using var s3 = new AmazonS3Client();
        var putRequest = new PutObjectRequest
        {
            BucketName = $"aircraft-bucket-{accountId}-{region}",
            Key = key,
            ContentBody = jsonString
        };
        var s3Response = await s3.PutObjectAsync(putRequest);
        if (s3Response.HttpStatusCode is not HttpStatusCode.OK)
        {
            throw new InvalidOperationException("Failed to upload seat layout to S3");
        }
        Console.WriteLine($"AWS S3 upload of file {path} to bucket aircraft-bucket-{accountId}-{region} completed successfully");
    }
    public static async Task ReadyAsync(HttpClient client)
    {
        var uri = new Uri("aircraft", UriKind.Relative);
        var response = await client.GetAsync(uri);
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Request to {uri} returned status code {response.StatusCode}. The Aircraft service may not be ready yet.");
        }
    }
}
