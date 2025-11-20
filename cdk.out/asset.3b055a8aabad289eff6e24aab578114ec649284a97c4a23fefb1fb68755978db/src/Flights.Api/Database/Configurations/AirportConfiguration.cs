using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Database.Configurations;

internal sealed class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        builder.Property(a => a.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(a => a.IcaoCode)
            .IsRequired()
            .HasMaxLength(4);
        builder.Property(a => a.IataCode)
            .IsRequired()
            .HasMaxLength(3);
    }
}
