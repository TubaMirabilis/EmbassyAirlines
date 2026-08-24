using Microsoft.EntityFrameworkCore;

namespace Shared.Npgsql;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyOutboxConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.HasDbFunction(() => DatabaseClock.UtcNow())
                    .HasName("now")
                    .HasSchema(null)
                    .IsBuiltIn();
        return modelBuilder;
    }
}
