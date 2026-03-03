using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Flights.Infrastructure.Database;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
#pragma warning disable S2068
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=DummyDb;Username=FakeUser;Password=FakePassword",
                o => o.UseNodaTime())
            .UseSnakeCaseNamingConvention();
        return new ApplicationDbContext(builder.Options);
    }
#pragma warning restore S2068
}
