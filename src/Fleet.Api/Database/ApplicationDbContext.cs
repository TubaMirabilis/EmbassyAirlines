using System.Text.Json;
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        modelBuilder.Entity<Aircraft>().Property(a => a.Registration).HasMaxLength(12).HasColumnType("varchar(12)");
        modelBuilder.Entity<Aircraft>().Property(a => a.Status).HasConversion<string>().HasMaxLength(20).HasColumnType("varchar(20)");
        modelBuilder.Entity<Aircraft>().Property(a => a.Location).HasMaxLength(4).HasColumnType("varchar(4)");
        modelBuilder.Entity<Aircraft>().Property(a => a.Model).HasMaxLength(50).HasColumnType("varchar(50)");
        modelBuilder.Entity<Aircraft>().Property(a => a.Type).HasConversion<string>().HasMaxLength(20).HasColumnType("varchar(20)");
        modelBuilder.Entity<Aircraft>().Property(a => a.TypeDesignator).HasMaxLength(4).HasColumnType("varchar(4)");
        modelBuilder.Entity<Aircraft>().Property(a => a.EngineModel).HasMaxLength(50).HasColumnType("varchar(50)");
        // SeatingConfiguration is a dictionary, so we need to configure it as a JSON column. ef core will handle serialization/deserialization
        modelBuilder.Entity<Aircraft>().Property(a => a.SeatingConfiguration).HasColumnType("json")
            .HasConversion(
                v => JsonSerializer.Serialize<Dictionary<string, short>>(v, options),
                v => JsonSerializer.Deserialize<Dictionary<string, short>>(v, options) ?? new Dictionary<string, short>()
            );
    }

    public DbSet<Aircraft> Aircraft { get; set; }
}
