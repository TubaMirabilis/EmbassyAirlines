using Fleet.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fleet.Api.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Aircraft> Aircraft { get; set; }
}
