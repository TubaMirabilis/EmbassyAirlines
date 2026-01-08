#:package AWSSDK.S3
#:package CliWrap
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using CliWrap;
using CliWrap.Buffered;

var baseAddress = args.Length > 0 ? args[0] : "https://embassyairlines.com/api/";
using var client = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};
var endpoints = new[] { "airports", "aircraft", "flights" };
foreach (var endpoint in endpoints)
{
    Console.WriteLine($"Testing endpoint: {endpoint} using method GET");
    var response = await client.GetAsync(new Uri(endpoint, UriKind.Relative));
    if (response.StatusCode is not HttpStatusCode.OK)
    {
        throw new InvalidOperationException($"Request to {endpoint} failed with status code {response.StatusCode}");
    }
}
var path = "Resources/Layouts/B78X.json";
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
    Key = "seat-layouts/B78X.json",
    ContentBody = jsonString
};
var s3Response = await s3.PutObjectAsync(putRequest);
if (s3Response.HttpStatusCode is not HttpStatusCode.OK)
{
    throw new InvalidOperationException("Failed to upload seat layout to S3");
}
Console.WriteLine($"AWS S3 upload of file {path} to bucket aircraft-bucket-{accountId}-{region} completed successfully");