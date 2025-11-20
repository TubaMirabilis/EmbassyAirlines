using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aircraft.Api.Lambda.Database.Configurations;

internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder) =>
        builder.Property(s => s.Type).HasConversion(new EnumToStringConverter<SeatType>()).HasMaxLength(24).IsUnicode(false).IsRequired();
}
