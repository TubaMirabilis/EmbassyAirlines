using System.Text.Json;
using AWS.Messaging;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NodaTime;
using Testcontainers.PostgreSql;

[assembly: CaptureConsole]
namespace Flights.Api.Lambda.FunctionalTests;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().WithImage("postgres")
                                                                               .WithDatabase("flights")
                                                                               .WithUsername("flights")
                                                                               .WithPassword("flights")
                                                                               .WithExposedPort(5432)
                                                                               .Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("FunctionalTests");
        builder.UseSetting("SNS:AircraftAssignedToFlightTopicArn", "testAircraftAssignedToFlightTopicArn");
        builder.UseSetting("SNS:FlightPricingAdjustedTopicArn", "testFlightPricingAdjustedTopicArn");
        builder.UseSetting("SNS:FlightRescheduledTopicArn", "testFlightRescheduledTopicArn");
        builder.UseSetting("SNS:FlightScheduledTopicArn", "testFlightScheduledTopicArn");
        builder.UseSetting("SNS:FlightCancelledTopicArn", "testFlightCancelledTopicArn");
        builder.UseSetting("SNS:FlightArrivedTopicArn", "testFlightArrivedTopicArn");
        builder.UseSetting("SNS:FlightDelayedTopicArn", "testFlightDelayedTopicArn");
        builder.UseSetting("SNS:FlightMarkedAsEnRouteTopicArn", "testFlightMarkedAsEnRouteTopicArn");
        builder.UseSetting("SNS:FlightMarkedAsDelayedEnRouteTopicArn", "testFlightMarkedAsDelayedEnRouteTopicArn");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMessagePublisher>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddSingleton<IMessagePublisher, FakeMessagePublisher>();
            services.AddSingleton(_ => new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_dbContainer.GetConnectionString(), x =>
            {
                x.MigrationsHistoryTable("__EFMigrationsHistory", "flights");
                x.UseNodaTime();
                x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .UseSnakeCaseNamingConvention()
            .LogTo(Console.WriteLine, LogLevel.Warning));
        });
    }
    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
        IncheonAirport = Airport.Create(new AirportCreationArgs
        {
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            IataCode = "ICN",
            IcaoCode = "RKSI",
            Id = Guid.NewGuid(),
            Name = "Incheon International Airport",
            TimeZoneId = "Asia/Seoul"
        });
        SchipolAirport = Airport.Create(new AirportCreationArgs
        {
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            IataCode = "AMS",
            IcaoCode = "EHAM",
            Id = Guid.NewGuid(),
            Name = "Schipol Airport",
            TimeZoneId = "Europe/Amsterdam"
        });
        Aircraft1 = Aircraft.Create(Guid.NewGuid(), "C-FJRN", "B78X", SystemClock.Instance.GetCurrentInstant());
        Aircraft2 = Aircraft.Create(Guid.NewGuid(), "C-FJRO", "B78X", SystemClock.Instance.GetCurrentInstant());
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Airports.AddRange(IncheonAirport, SchipolAirport);
        dbContext.Aircraft.AddRange(Aircraft1, Aircraft2);
        await dbContext.SaveChangesAsync();
    }
    public new async ValueTask DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
    internal Airport? IncheonAirport { get; private set; }
    internal Airport? SchipolAirport { get; private set; }
    internal Aircraft? Aircraft1 { get; private set; }
    internal Aircraft? Aircraft2 { get; private set; }
}
