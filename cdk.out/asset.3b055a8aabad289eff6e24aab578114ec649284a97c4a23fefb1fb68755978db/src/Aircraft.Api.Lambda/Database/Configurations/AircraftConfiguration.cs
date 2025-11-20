using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aircraft.Api.Lambda.Database.Configurations;

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
        builder.ComplexProperty(a => a.DryOperatingWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.MaximumTakeoffWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.MaximumLandingWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.MaximumZeroFuelWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.MaximumFuelWeight, b => b.Property(e => e.Kilograms).IsRequired());
    }
}
