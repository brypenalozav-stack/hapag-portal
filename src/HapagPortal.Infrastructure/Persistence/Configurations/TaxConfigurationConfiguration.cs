using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class TaxConfigurationConfiguration : IEntityTypeConfiguration<TaxConfiguration>
{
    public void Configure(EntityTypeBuilder<TaxConfiguration> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("TaxConfigurations");

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.ServiceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TaxName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.TaxRate)
            .HasPrecision(8, 4);

        builder.HasIndex(e => new { e.Country, e.ServiceType })
            .IsUnique();
    }
}
