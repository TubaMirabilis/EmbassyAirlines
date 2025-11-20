using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using AWS.Messaging;
using Flights.Api.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;

[assembly: CaptureConsole]
namespace Flights.Api.FunctionalTests;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder().Build();
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().WithImage("postgres")
                                                                               .WithDatabase("flights")
                                                                               .WithUsername("flights")
                                                                               .WithPassword("flights")
                                                                               .WithExposedPort(5432)
                                                                               .Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SNS:AircraftAssignedToFlightTopicArn", "testAircraftAssignedToFlightTopicArn");
        builder.UseSetting("SNS:FlightPricingAdjustedTopicArn", "testFlightPricingAdjustedTopicArn");
        builder.UseSetting("SNS:FlightRescheduledTopicArn", "testFlightRescheduledTopicArn");
        builder.UseSetting("SQS:QueueUrl", "testQueueUrl");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("AWS:BucketName", "embassy-airlines");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMessagePublisher>();
            services.RemoveAll<IAmazonSQS>();
            var credentials = new BasicAWSCredentials("test-access-key", "test-secret-key");
            var config = new AmazonSQSConfig
            {
                ServiceURL = _localStackContainer.GetConnectionString()
            };
            services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(credentials, config));
            services.AddSingleton<IMessagePublisher, FakeMessagePublisher>();
            services.AddSingleton<JsonSerializerOptions>(_ => new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        });
    }
    public async ValueTask InitializeAsync()
    {
        await _localStackContainer.StartAsync();
        await _dbContainer.StartAsync();
        IncheonAirport = Airport.Create(new AirportCreationArgs
        {
            IataCode = "ICN",
            IcaoCode = "RKSI",
            Id = Guid.NewGuid(),
            Name = "Incheon International Airport",
            TimeZoneId = "Asia/Seoul"
        });
        SchipolAirport = Airport.Create(new AirportCreationArgs
        {
            IataCode = "AMS",
            IcaoCode = "EHAM",
            Id = Guid.NewGuid(),
            Name = "Schipol Airport",
            TimeZoneId = "Europe/Amsterdam"
        });
        Aircraft1 = Aircraft.Create(Guid.NewGuid(), "C-FJRN", "B78X");
        Aircraft2 = Aircraft.Create(Guid.NewGuid(), "C-FJRO", "B78X");
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Airports.AddRange(IncheonAirport, SchipolAirport);
        dbContext.Aircraft.AddRange(Aircraft1, Aircraft2);
        await dbContext.SaveChangesAsync();
    }
    public new async ValueTask DisposeAsync()
    {
        await _localStackContainer.StopAsync();
        await _dbContainer.StopAsync();
        await _localStackContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
    internal Airport? IncheonAirport { get; private set; }
    internal Airport? SchipolAirport { get; private set; }
    internal Aircraft? Aircraft1 { get; private set; }
    internal Aircraft? Aircraft2 { get; private set; }
}
