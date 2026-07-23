using Airports.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

[assembly: CaptureConsole]
namespace Airports.Api.Lambda.FunctionalTests;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18").Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("FunctionalTests");
        builder.UseSetting("SNS:AirportCreatedTopicArn", "testAirportCreatedTopicArn");
        builder.UseSetting("SNS:AirportUpdatedTopicArn", "testAirportUpdatedTopicArn");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_dbContainer.GetConnectionString(), x =>
            {
                x.MigrationsHistoryTable("__EFMigrationsHistory", "airports");
                x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .UseSnakeCaseNamingConvention()
            .LogTo(Console.WriteLine, LogLevel.Warning));
        });
    }
    public async ValueTask InitializeAsync() => await _dbContainer.StartAsync();
    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
