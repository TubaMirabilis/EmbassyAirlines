using Microsoft.EntityFrameworkCore;

namespace Shared.Npgsql.UnitTests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }
}
