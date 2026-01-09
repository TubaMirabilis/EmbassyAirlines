#:project ../src/Shared
#:package AWSSDK.S3
#:package CliWrap
#:package NodaTime
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using CliWrap;
using CliWrap.Buffered;
using Shared.Contracts;
using SmokeTests;

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
var jsonContext = new SmokeTestJsonContext();
var req1 = new CreateOrUpdateAirportDto("RKSI", "ICN", "Incheon International Airport", "Asia/Seoul");
Console.WriteLine($"Attempting to create resource for {req1.Name} ({req1.IataCode})");
var res1 = await client.PostAsJsonAsync("airports", req1, jsonContext.CreateOrUpdateAirportDto);
res1.EnsureSuccessStatusCode();
var req2 = new CreateOrUpdateAirportDto("EHAM", "AMS", "Schipol Airport", "Europe/Amsterdam");
Console.WriteLine($"Attempting to create resource for {req2.Name} ({req2.IataCode})");
var res2 = await client.PostAsJsonAsync("airports", req2, jsonContext.CreateOrUpdateAirportDto);
res2.EnsureSuccessStatusCode();
var req3 = new CreateAircraftDto("PH-JRN", "B78X", 135500, "Parked", 254011, "RKSI", null, 201848, 192777, 101522);
Console.WriteLine($"Attempting to create aircraft with tail number {req3.TailNumber}");
var res3 = await client.PostAsJsonAsync("aircraft", req3, jsonContext.CreateAircraftDto);
res3.EnsureSuccessStatusCode();

namespace SmokeTests
{
    [JsonSerializable(typeof(AirportDto))]
    [JsonSerializable(typeof(AircraftDto))]
    [JsonSerializable(typeof(CreateOrUpdateAirportDto))]
    [JsonSerializable(typeof(CreateAircraftDto))]
    [JsonSerializable(typeof(FlightDto))]
    [JsonSerializable(typeof(ScheduleFlightDto))]
    internal sealed partial class SmokeTestJsonContext : JsonSerializerContext
    {
    }
}
