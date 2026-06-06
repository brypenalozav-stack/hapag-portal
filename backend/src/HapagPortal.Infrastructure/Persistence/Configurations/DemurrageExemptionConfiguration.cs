using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class DemurrageExemptionConfiguration : IEntityTypeConfiguration<DemurrageExemption>
{
    public void Configure(EntityTypeBuilder<DemurrageExemption> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("DemurrageExemptions");

        builder.Property(e => e.ClientName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.TaxId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.TaxId, e.Country })
            .IsUnique();
    }
}
