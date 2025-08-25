using System.Text.Json;
using Flights.Api.Database;
using Flights.Api.FunctionalTests.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

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
            var descriptors = services.Where(d => d.IsMassTransitService())
.ToList();
            foreach (var d in descriptors)
            {
                services.Remove(d);
            }
            services.AddMassTransitTestHarness();
        });
    }
    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
        IncheonAirport = Airport.Create(Guid.NewGuid(), "Asia/Seoul", "ICN", "RKSI", "Incheon International Airport");
        SchipolAirport = Airport.Create(Guid.NewGuid(), "Europe/Amsterdam", "AMS", "EHAM", "Schipol Airport");
        Aircraft = Aircraft.Create(Guid.NewGuid(), "C-FJRN", "B78X");
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Airports.AddRange(IncheonAirport, SchipolAirport);
        dbContext.Aircraft.Add(Aircraft);
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
    internal Aircraft? Aircraft { get; set; }
}
