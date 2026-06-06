using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class LocalChargeConfiguration : IEntityTypeConfiguration<LocalCharge>
{
    public void Configure(EntityTypeBuilder<LocalCharge> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("LocalCharges");

        builder.Property(e => e.ChargeType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.TaxRate)
            .HasPrecision(8, 4);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);
    }
}
