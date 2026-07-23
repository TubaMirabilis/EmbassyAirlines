using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared;

namespace Airports.Infrastructure.Database.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.Property(o => o.Name)
               .IsRequired()
               .IsUnicode(false)
               .HasMaxLength(256);
        builder.Property(o => o.Content)
               .IsRequired();
        builder.Property(o => o.CreatedOnUtc)
               .HasColumnType("timestamp with time zone")
               .IsRequired();
        builder.Property(o => o.ProcessedOnUtc)
               .HasColumnType("timestamp with time zone")
               .IsRequired(false);
        builder.Property(o => o.Error)
               .IsRequired(false);
        builder.Property(o => o.RetryCount)
               .IsRequired();
        builder.Property(o => o.NextAttemptOnUtc)
               .HasColumnType("timestamp with time zone")
               .IsRequired(false);
        builder.Property(o => o.DeadLetteredOnUtc)
               .HasColumnType("timestamp with time zone")
               .IsRequired(false);
        builder.HasIndex(o => o.CreatedOnUtc)
               .HasDatabaseName("ix_outbox_messages_unprocessed")
               .HasFilter("processed_on_utc IS NULL AND dead_lettered_on_utc IS NULL");
    }
}
