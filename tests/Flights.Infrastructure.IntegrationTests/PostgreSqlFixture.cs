using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Npgsql;
using Testcontainers.PostgreSql;

namespace Flights.Infrastructure.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();
    public string ConnectionString => _postgres.GetConnectionString();
    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }
    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "flights");
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            x.UseNodaTime();
        })
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(new InsertOutboxMessagesInterceptor())
            .Options;
        return new ApplicationDbContext(options);
    }
}
