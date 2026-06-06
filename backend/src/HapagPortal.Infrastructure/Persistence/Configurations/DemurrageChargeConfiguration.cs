using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class DemurrageChargeConfiguration : IEntityTypeConfiguration<DemurrageCharge>
{
    public void Configure(EntityTypeBuilder<DemurrageCharge> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("DemurrageCharges");

        builder.Property(e => e.ContainerNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.DailyRate)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.ExemptReason)
            .HasMaxLength(500);
    }
}
