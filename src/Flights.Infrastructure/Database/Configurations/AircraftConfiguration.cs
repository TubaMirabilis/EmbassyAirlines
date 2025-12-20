using Flights.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Infrastructure.Database.Configurations;

internal sealed class AircraftConfiguration : IEntityTypeConfiguration<Aircraft>
{
    public void Configure(EntityTypeBuilder<Aircraft> builder)
    {
        builder.Property(a => a.TailNumber)
            .IsRequired()
            .HasMaxLength(12);
        builder.Property(a => a.EquipmentCode)
            .IsRequired()
            .HasMaxLength(4);
    }
}
