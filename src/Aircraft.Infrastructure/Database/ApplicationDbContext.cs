using Aircraft.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Aircraft.Infrastructure.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
    public DbSet<Core.Models.Aircraft> Aircraft { get; set; } = null!;
    public DbSet<Seat> Seats { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assembly = typeof(ApplicationDbContext).Assembly;
        modelBuilder.HasDefaultSchema("aircraft");
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
