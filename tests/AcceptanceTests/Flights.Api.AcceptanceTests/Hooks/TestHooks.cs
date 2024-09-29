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
public class TestHooks
{
    private readonly IObjectContainer _objectContainer;
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
    }

    [BeforeScenario]
    public async Task RegisterServices()
    {
        await _dbContainer.StartAsync();
        var factory = GetWebApplicationFactory();
        _objectContainer.RegisterInstanceAs(factory);
        using var scope = _objectContainer.Resolve<WebApplicationFactory<Program>>().Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

#pragma warning disable CA2000
    private WebApplicationFactory<Program> GetWebApplicationFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_dbContainer.GetConnectionString()).UseSnakeCaseNamingConvention());
                }));
#pragma warning restore CA2000

    [AfterScenario]
    public async Task DisposeServices()
    {
        await _dbContainer.StopAsync();
    }
}
