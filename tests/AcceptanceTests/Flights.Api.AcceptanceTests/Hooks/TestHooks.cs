using System.Text.Json;
using BoDi;
using Flights.Api.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TechTalk.SpecFlow;
using Testcontainers.PostgreSql;

namespace Flights.Api.AcceptanceTests.Hooks;

[Binding]
internal sealed class TestHooks : IAsyncDisposable
{
    private readonly IObjectContainer _objectContainer;
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly PostgreSqlContainer _dbContainer;
    public TestHooks(IObjectContainer objectContainer)
    {
        _objectContainer = objectContainer;
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres")
            .WithDatabase("flights")
            .WithUsername("admin")
            .WithPassword("admin")
            .WithExposedPort(5432)
            .Build();
        _factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            // Add scoped JsonSerializerOptions to the service collection
            services.TryAddScoped(_ => new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            var connectionString = _dbContainer.GetConnectionString();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.UseNodaTime())
                       .UseSnakeCaseNamingConvention());
        }));
    }

    [BeforeScenario]
    public async Task RegisterServices()
    {
        await _dbContainer.StartAsync();
        _objectContainer.RegisterInstanceAs(_factory);
        using var scope = _objectContainer.Resolve<WebApplicationFactory<Program>>()
                                          .Services
                                          .CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await dbContext.Database
                       .MigrateAsync();
    }

    [AfterScenario]
    public async ValueTask DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
