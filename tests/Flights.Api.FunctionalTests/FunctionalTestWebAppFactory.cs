using System.Text.Json;
using Flights.Api.Database;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

[assembly: CaptureConsole]
namespace Flights.Api.FunctionalTests;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().WithImage("postgres").WithDatabase("mastermind").WithUsername("mastermind").WithPassword("mastermind").WithExposedPort(5432).Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("MassTransit:Scope", "embassy-airlines");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("AWS:BucketName", "embassy-airlines");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<JsonSerializerOptions>(_ => new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
  services.AddMassTransitTestHarness();
        });
    }
    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
        IncheonAirport = Airport.Create(Guid.NewGuid(), "Asia/Seoul", "ICN", "RKSI", "Incheon International Airport");
        SchipolAirport = Airport.Create(Guid.NewGuid(), "Europe/Amsterdam", "AMS", "EHAM", "Schipol Airport");
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
