using Microsoft.EntityFrameworkCore;

namespace Aircraft.Api.Lambda.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
    public DbSet<Aircraft> Aircraft { get; set; } = null!;
    public DbSet<Seat> Seats { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assembly = typeof(ApplicationDbContext).Assembly;
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
