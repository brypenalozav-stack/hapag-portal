using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class PaymentDetailConfiguration : IEntityTypeConfiguration<PaymentDetail>
{
    public void Configure(EntityTypeBuilder<PaymentDetail> builder)
    {
        builder.ToTable("PaymentDetails");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ConceptType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2);

        // Fase 1 Ola D: fuente, BL, RUT de facturación (M5-09) y monto de origen (M5-08).
        builder.Property(e => e.ItemType).HasMaxLength(30);
        builder.Property(e => e.BlNumber).HasMaxLength(50);
        builder.Property(e => e.BookingNumber).HasMaxLength(50);
        builder.Property(e => e.BillingTaxId).HasMaxLength(20);
        builder.Property(e => e.BillingName).HasMaxLength(200);
        builder.Property(e => e.OriginalAmount).HasPrecision(18, 2);
        builder.Property(e => e.OriginalCurrency).HasMaxLength(5);
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 6);

        builder.HasIndex(e => new { e.ItemType, e.SourceId });
    }
}
