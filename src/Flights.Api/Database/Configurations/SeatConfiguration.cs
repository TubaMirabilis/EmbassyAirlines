using Flights.Api.Domain.Seats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Flights.Api.Database.Configurations;

internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property<byte[]>("Version").IsRowVersion();
        builder.HasIndex(b => new { b.FlightId, b.SeatNumber })
               .IsUnique();
        builder.HasIndex(b => new { b.FlightId, b.PassengerId })
               .IsUnique()
               .HasFilter("passenger_id IS NOT NULL");
        builder.Property(b => b.CreatedAt)
               .IsRequired();
        builder.Property(b => b.UpdatedAt)
               .IsRequired();
        builder.Property(b => b.SeatNumber)
               .IsRequired()
               .HasMaxLength(3)
               .IsUnicode(false);
        builder.Property(b => b.Price)
               .HasColumnType("NUMERIC(9,2)")
               .IsRequired();
        builder.Property(a => a.SeatType)
               .HasConversion(new EnumToStringConverter<SeatType>())
               .IsRequired()
               .HasMaxLength(20)
               .IsUnicode(false);
    }
}
