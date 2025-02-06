using System.Text.Json;
using Flights.Api.Database;
using Flights.Api.FunctionalTests.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Flights.Api.FunctionalTests.Abstractions;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("runtrackr")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureTestServices(services =>
                                                                              {
                                                                                  services.AddScoped<JsonSerializerOptions>(_ => new JsonSerializerOptions
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
                                                                                  services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                                                                                  services.AddDbContext<ApplicationDbContext>(options =>
                                                                                      options
                                                                                          .UseNpgsql(_dbContainer.GetConnectionString())
                                                                                          .UseSnakeCaseNamingConvention());
                                                                              });
    public async Task InitializeAsync() => await _dbContainer.StartAsync();
    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}
