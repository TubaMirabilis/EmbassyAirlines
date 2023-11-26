using EmbassyAirlines.Domain;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmbassyAirlines.Infrastructure;

internal sealed class ApplicationDbContext : DbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Aircraft> Aircraft { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aircraft>()
            .Property(a => a.Registration)
            .HasMaxLength(16)
            .HasColumnType("varchar(16)");
        modelBuilder.Entity<Aircraft>()
            .Property(a => a.Model)
            .HasMaxLength(64)
            .HasColumnType("varchar(64)");
        modelBuilder.Entity<Aircraft>()
            .Property(a => a.Type)
            .HasMaxLength(16)
            .HasColumnType("varchar(16)");
    }
}