using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ChargeSettlementConfiguration : IEntityTypeConfiguration<ChargeSettlement>
{
    public void Configure(EntityTypeBuilder<ChargeSettlement> builder)
    {
        builder.ToTable("ChargeSettlements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kind).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.PaymentNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReceiptNumber).HasMaxLength(50);
        builder.Property(e => e.PayerTaxId).HasMaxLength(20);
        builder.Property(e => e.PayerName).HasMaxLength(200);
        builder.Property(e => e.BillingTaxId).HasMaxLength(20);
        builder.Property(e => e.BillingName).HasMaxLength(200);
        builder.Property(e => e.BlNumber).HasMaxLength(50);
        builder.Property(e => e.BookingNumber).HasMaxLength(50);
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ItemType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ConceptCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.PaidAmount).HasPrecision(18, 2);
        builder.Property(e => e.PaidCurrency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.MatchedBy).HasMaxLength(256);
        builder.Property(e => e.MatchNote).HasMaxLength(500);

        // Un registro por ítem pagado: la liberación (NF-03) es idempotente.
        builder.HasIndex(e => e.PaymentDetailId).IsUnique();
        builder.HasIndex(e => new { e.BillOfLadingId, e.ConceptCode, e.Status });
        builder.HasIndex(e => e.PayerOrganizationId);
        builder.HasIndex(e => e.MatchedInvoiceId);
        builder.HasIndex(e => new { e.ItemType, e.SourceId });
    }
}

internal sealed class DepositProofConfiguration : IEntityTypeConfiguration<DepositProof>
{
    public void Configure(EntityTypeBuilder<DepositProof> builder)
    {
        builder.ToTable("DepositProofs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.StorageKey).HasMaxLength(300);
        builder.Property(e => e.ContentHash).HasMaxLength(64);
        builder.Property(e => e.BankName).HasMaxLength(100);
        builder.Property(e => e.BankReference).HasMaxLength(60);
        builder.Property(e => e.DepositAmount).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.UploadedBy).HasMaxLength(256).IsRequired();
        builder.Property(e => e.ReviewedBy).HasMaxLength(256);
        builder.Property(e => e.ReviewNotes).HasMaxLength(500);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);

        builder.HasIndex(e => new { e.PaymentId, e.UploadedAt });
        builder.HasIndex(e => new { e.Status, e.UploadedAt });

        builder.HasOne(e => e.Payment)
            .WithMany()
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CreditImputationRuleConfiguration : IEntityTypeConfiguration<CreditImputationRule>
{
    public void Configure(EntityTypeBuilder<CreditImputationRule> builder)
    {
        builder.ToTable("CreditImputationRules");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ConceptCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.NexusCreditConcept).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasIndex(e => new { e.Country, e.ConceptCode })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}

internal sealed class InvoiceReissueConfiguration : IEntityTypeConfiguration<InvoiceReissue>
{
    public void Configure(EntityTypeBuilder<InvoiceReissue> builder)
    {
        builder.ToTable("InvoiceReissues");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.OriginalSiiNumber).HasMaxLength(30);
        builder.Property(e => e.OriginalSourceNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.OriginalTaxId).HasMaxLength(20).IsRequired();
        builder.Property(e => e.OriginalLegalName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.VatLossAmount).HasPrecision(18, 2);
        builder.Property(e => e.FeeAmount).HasPrecision(18, 2);
        builder.Property(e => e.FeeTaxAmount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(5);
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 6);
        builder.Property(e => e.AcceptorEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.AcceptanceStatus).HasMaxLength(20);
        builder.Property(e => e.AcceptanceTokenHash).HasMaxLength(64);
        builder.Property(e => e.AcceptedByName).HasMaxLength(200);
        builder.Property(e => e.AcceptedByTaxId).HasMaxLength(20);
        builder.Property(e => e.AcceptedFromAddress).HasMaxLength(64);
        builder.Property(e => e.DeclineReason).HasMaxLength(500);

        builder.HasIndex(e => e.ServiceRequestId).IsUnique();
        builder.HasIndex(e => e.OriginalInvoiceId);
        builder.HasIndex(e => e.AcceptanceTokenHash)
            .IsUnique()
            .HasFilter("\"AcceptanceTokenHash\" IS NOT NULL");

        builder.HasOne(e => e.ServiceRequest)
            .WithMany()
            .HasForeignKey(e => e.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
