using Microsoft.EntityFrameworkCore;
using Fleet.Api.Entities;

namespace Fleet.Api.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Aircraft> Aircraft { get; set; }
}
