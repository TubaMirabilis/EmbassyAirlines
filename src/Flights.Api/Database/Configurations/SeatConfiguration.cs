using Flights.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Flights.Api.Configurations;

internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.CreatedAt)
               .IsRequired();
        builder.Property(b => b.UpdatedAt)
               .IsRequired();
        builder.Property(b => b.SeatNumber)
               .IsRequired()
               .HasMaxLength(3)
               .IsUnicode(false);
        builder.Property(b => b.Price)
               .IsRequired();
        builder.Property(a => a.SeatType)
               .HasConversion(new EnumToStringConverter<SeatType>())
               .IsRequired()
               .HasMaxLength(20)
               .IsUnicode(false);
    }
}
