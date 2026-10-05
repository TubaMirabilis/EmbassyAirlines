using Airports.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Shared.Npgsql;
using Testcontainers.PostgreSql;

[assembly: CaptureConsole]
namespace Airports.Api.Lambda.FunctionalTests;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18").Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureTestServices(services => services.AddDatabaseConnection<ApplicationDbContext>(_dbContainer.GetConnectionString(), useNodaTime: false, schema: "airports"));
    public async ValueTask InitializeAsync() => await _dbContainer.StartAsync();
    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
