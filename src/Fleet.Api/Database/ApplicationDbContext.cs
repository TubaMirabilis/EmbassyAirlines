using Fleet.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fleet.Api.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<Aircraft>().Property(a => a.Registration).HasMaxLength(12).HasColumnType("varchar(12)");
        modelBuilder.Entity<Aircraft>().Property(a => a.Status).HasMaxLength(20).HasColumnType("varchar(20)");
    }   

    public DbSet<Aircraft> Aircraft { get; set; }
}
