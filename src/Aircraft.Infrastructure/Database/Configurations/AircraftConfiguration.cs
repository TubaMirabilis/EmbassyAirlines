using Aircraft.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aircraft.Infrastructure.Database.Configurations;

internal sealed class AircraftConfiguration : IEntityTypeConfiguration<Core.Models.Aircraft>
{
       public void Configure(EntityTypeBuilder<Core.Models.Aircraft> builder)
       {
              builder.Property(a => a.TailNumber)
                     .IsRequired()
                      .IsUnicode(false)
                     .HasMaxLength(12);
              builder.Property(a => a.ParkedAt)
                     .HasMaxLength(4)
                      .IsUnicode(false)
                      .IsRequired(false);
              builder.Property(a => a.EnRouteTo)
                     .HasMaxLength(4)
                      .IsUnicode(false)
                      .IsRequired(false);
              builder.Property(a => a.EquipmentCode)
                     .IsRequired()
                     .IsUnicode(false)
                     .HasMaxLength(4);
              builder.Property(a => a.Status).HasConversion(new EnumToStringConverter<Status>()).HasMaxLength(12).IsUnicode(false).IsRequired();
              builder.ComplexProperty(a => a.DryOperatingWeight, b => b.Property(e => e.Kilograms).IsRequired());
              builder.ComplexProperty(a => a.MaximumTakeoffWeight, b => b.Property(e => e.Kilograms).IsRequired());
              builder.ComplexProperty(a => a.MaximumLandingWeight, b => b.Property(e => e.Kilograms).IsRequired());
              builder.ComplexProperty(a => a.MaximumZeroFuelWeight, b => b.Property(e => e.Kilograms).IsRequired());
              builder.ComplexProperty(a => a.MaximumFuelWeight, b => b.Property(e => e.Kilograms).IsRequired());
       }
}
