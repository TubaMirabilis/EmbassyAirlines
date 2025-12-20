using System.Text.Json;
using Aircraft.Api.Lambda.Database;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AWS.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;

[assembly: CaptureConsole]
namespace Aircraft.Api.Lambda.FunctionalTests;

public sealed class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder().Build();
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().WithImage("postgres")
                                                                               .WithDatabase("aircraft")
                                                                               .WithUsername("aircraft")
                                                                               .WithPassword("aircraft")
                                                                               .WithExposedPort(5432)
                                                                               .Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("FunctionalTests");
        builder.UseSetting("SNS:AircraftCreatedTopicArn", "testAircraftCreatedTopicArn");
        builder.UseSetting("S3:BucketName", "embassy-airlines");
        builder.ConfigureTestServices(services =>
        {
            var credentials = new BasicAWSCredentials("test-access-key", "test-secret-key");
            services.AddSingleton<JsonSerializerOptions>(_ => new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            services.RemoveAll<IAmazonS3>();
            services.RemoveAll<IMessagePublisher>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            var config = new AmazonS3Config
            {
                ServiceURL = _localStackContainer.GetConnectionString()
            };
            services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(credentials, config));
            services.AddSingleton<IMessagePublisher, FakeMessagePublisher>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_dbContainer.GetConnectionString(), x =>
            {
                x.MigrationsHistoryTable("__EFMigrationsHistory", "aircraft");
                x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .UseSnakeCaseNamingConvention()
            .LogTo(Console.WriteLine, LogLevel.Warning));
        });
    }
    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _localStackContainer.StartAsync();
        var client = Services.GetRequiredService<IAmazonS3>();
        await client.PutBucketAsync("embassy-airlines");

        var putRequest = new PutObjectRequest
        {
            BucketName = "embassy-airlines",
            Key = "seat-layouts/B78X.json",
            ContentBody = SeatLayoutDefinitionJson,
            DisableDefaultChecksumValidation = true
        };
        await client.PutObjectAsync(putRequest);
    }
    public new async ValueTask DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _localStackContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _localStackContainer.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
    private static string SeatLayoutDefinitionJson => """
    {
        "EquipmentType": "B78X",
        "BusinessRows": {
            "1-17": {
                "Seats": ["A", "K"], "SeatType": "Business", "EveryNthRowOnly": 2
            },
            "2-18": {
                "Seats": ["D", "F"], "SeatType": "Business", "EveryNthRowOnly": 2
            }
        },
        "EconomyRows": {
            "19-49": {
                "Seats": ["A", "B", "C", "D", "E", "F", "G", "H", "J"],
                "SeatType": "Economy"
            },
            "50": {
                "Seats": ["A", "B", "C", "D", "F", "G", "H", "J"],
                "SeatType": "Economy"
            },
            "51-52": {
                "Seats": ["A", "B", "D", "E", "F", "G", "J"],
                "SeatType": "Economy"
            }
        }
    }
    """;
}
