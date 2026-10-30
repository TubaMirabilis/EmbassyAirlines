using Aircraft.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aircraft.Infrastructure.IntegrationTests;

public sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly PostgreSqlFixture _postgres;
    private readonly IInterceptor[] _interceptors;

    public TestDbContextFactory(PostgreSqlFixture postgres, params IInterceptor[] interceptors)
    {
        _postgres = postgres;
        _interceptors = interceptors;
    }

    public ApplicationDbContext CreateDbContext() => _postgres.CreateDbContext(_interceptors);
}
