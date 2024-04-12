using System.Text.Json;
using Fleet.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Fleet.Api.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        var aircraftEntity = modelBuilder.Entity<Aircraft>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        aircraftEntity.Property(a => a.Registration).HasMaxLength(12).HasColumnType("varchar(12)");
        aircraftEntity.Property(a => a.AircraftStatus).HasConversion<string>().HasMaxLength(20)
            .HasColumnType("varchar(20)");
        aircraftEntity.Property(a => a.OperationalStatus).HasConversion<string>().HasMaxLength(20)
            .HasColumnType("varchar(20)");
        aircraftEntity.Property(a => a.Location).HasMaxLength(4).HasColumnType("varchar(4)");
        aircraftEntity.Property(a => a.Model).HasMaxLength(50).HasColumnType("varchar(50)");
        aircraftEntity.Property(a => a.Type).HasConversion<string>().HasMaxLength(20).HasColumnType("varchar(20)");
        aircraftEntity.Property(a => a.TypeDesignator).HasMaxLength(4).HasColumnType("varchar(4)");
        aircraftEntity.Property(a => a.EngineModel).HasMaxLength(50).HasColumnType("varchar(50)");
        aircraftEntity.Property(a => a.SeatingConfiguration).HasColumnType("json")
            .HasConversion(
                v => JsonSerializer.Serialize<Dictionary<string, short>>(v, options),
                v => JsonSerializer.Deserialize<Dictionary<string, short>>(v, options)
                    ?? new Dictionary<string, short>()
            ).Metadata.SetValueComparer(new ValueComparer<Dictionary<string, short>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToDictionary(kv => kv.Key, kv => kv.Value)
            ));
    }
    public DbSet<Aircraft> Aircraft { get; set; }
}
