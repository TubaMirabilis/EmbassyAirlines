using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Flights.Api.Database;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=DummyDb;Username=FakeUser;Password=FakePassword",
                o => o.UseNodaTime())
            .UseSnakeCaseNamingConvention();
        return new ApplicationDbContext(builder.Options);
    }
}
