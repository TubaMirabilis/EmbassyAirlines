using Flights.Api.Domain.Itineraries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Database.Configurations;

internal sealed class ItineraryConfiguration : IEntityTypeConfiguration<Itinerary>
{
    public void Configure(EntityTypeBuilder<Itinerary> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Navigation(b => b.Bookings)
               .AutoInclude();
        builder.Property(b => b.CreatedAt)
               .IsRequired();
        builder.Property(b => b.UpdatedAt)
               .IsRequired();
        builder.HasMany(b => b.Bookings)
               .WithOne()
               .HasForeignKey("ItineraryId")
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
