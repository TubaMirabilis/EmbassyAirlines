using Flights.Api.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Database.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
       public void Configure(EntityTypeBuilder<Booking> builder)
       {
              builder.HasKey(b => b.Id);
              builder.Property(b => b.CreatedAt)
                     .IsRequired();
              builder.Property(b => b.UpdatedAt)
                     .IsRequired();
              builder.HasMany(b => b.Passengers)
                     .WithOne()
                     .HasForeignKey(p => p.BookingId)
                     .IsRequired()
                     .OnDelete(DeleteBehavior.Cascade);
              builder.HasMany(b => b.Seats)
                     .WithOne()
                     .HasForeignKey(s => s.BookingId)
                     .IsRequired()
                     .OnDelete(DeleteBehavior.Cascade);
              builder.Property(b => b.LeadPassengerEmail)
                     .HasMaxLength(100)
                     .IsUnicode(false)
                     .IsRequired();
              builder.Property(b => b.Reference)
                     .HasMaxLength(10)
                     .IsUnicode(false)
                     .IsRequired();
       }
}
