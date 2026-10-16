using Aircraft.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aircraft.Infrastructure.Database.Configurations;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(x => x.EventId);

        builder.Property(x => x.EventId)
            .ValueGeneratedNever();

        builder.Property(x => x.ProcessedOnUtc)
            .IsRequired();
    }
}
