using Amazon;

var builder = DistributedApplication.CreateBuilder(args);
var awsSdkConfig = builder.AddAWSSDKConfig().WithRegion(RegionEndpoint.EUWest2);
var awsResources = builder.AddAWSCDKStack("AirportsServiceAWSResources").WithReference(awsSdkConfig);
await builder.Build().RunAsync();
