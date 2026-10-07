using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("Payments");

        builder.Property(e => e.PaymentNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.PaymentType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.PaymentMethod)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.ExchangeRate)
            .HasPrecision(18, 6);

        builder.Property(e => e.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.ConfirmedBy)
            .HasMaxLength(256);

        builder.Property(e => e.ExternalReference)
            .HasMaxLength(200);

        builder.Property(e => e.ReceiptNumber)
            .HasMaxLength(100);

        builder.Property(e => e.DepositProofUrl)
            .HasMaxLength(1000);

        // Fase 1 Ola D (M5-01, M5-03, M7-02, NF-01, NF-04, NF-12).
        builder.Property(e => e.Origin).HasMaxLength(20).IsRequired();
        builder.Property(e => e.IdempotencyKey).HasMaxLength(100);
        builder.Property(e => e.RequestFingerprint).HasMaxLength(200);
        builder.Property(e => e.PaymentMethodCode).HasMaxLength(40);
        builder.Property(e => e.ProviderKey).HasMaxLength(30);
        builder.Property(e => e.ProviderReference).HasMaxLength(200);
        builder.Property(e => e.ProviderTransactionId).HasMaxLength(200);
        builder.Property(e => e.RedirectUrl).HasMaxLength(1000);
        builder.Property(e => e.RedirectForm).HasMaxLength(8000);
        builder.Property(e => e.PayerTaxId).HasMaxLength(20);
        builder.Property(e => e.PayerName).HasMaxLength(200);
        builder.Property(e => e.FailureReason).HasMaxLength(50);
        builder.Property(e => e.SlipNumber).HasMaxLength(50);
        builder.Property(e => e.CancelledBy).HasMaxLength(256);
        builder.Property(e => e.CancelledByRole).HasMaxLength(20);
        builder.Property(e => e.CancellationReason).HasMaxLength(500);

        builder.HasIndex(e => e.PaymentNumber)
            .IsUnique();

        // NF-01: una clave de idempotencia por usuario.
        builder.HasIndex(e => new { e.CreatedByUserId, e.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.HasIndex(e => e.ExternalReference);
        builder.HasIndex(e => new { e.ClientId, e.PaymentDate });

        builder.HasMany(e => e.Details)
            .WithOne(e => e.Payment)
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
