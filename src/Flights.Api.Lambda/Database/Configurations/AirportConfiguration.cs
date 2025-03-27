using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Lambda.Database.Configurations;

internal sealed class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
       public void Configure(EntityTypeBuilder<Airport> builder)
       {
              builder.HasKey(b => b.Id);
              builder.Property(b => b.CreatedAt)
                     .IsRequired();
              builder.Property(b => b.UpdatedAt)
                     .IsRequired();
              builder.Property(b => b.Name)
                     .IsRequired()
                     .HasMaxLength(100);
              builder.Property(b => b.TimeZoneId)
                     .IsRequired()
                     .HasMaxLength(100);
              builder.Property(b => b.IataCode)
                     .IsRequired()
                     .HasMaxLength(3);
       }
}
