using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aircraft.Api.Lambda.Database;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=DummyDb;Username=FakeUser;Password=FakePassword")
            .UseSnakeCaseNamingConvention();
        return new ApplicationDbContext(builder.Options);
    }
}
