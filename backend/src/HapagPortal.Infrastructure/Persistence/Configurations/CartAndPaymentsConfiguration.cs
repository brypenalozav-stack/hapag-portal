using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.ConfigureBaseAuditableEntity();

        builder.HasIndex(e => new { e.UserId, e.OrganizationId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasMany(e => e.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ItemType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.BlNumber).HasMaxLength(50);
        builder.Property(e => e.BookingNumber).HasMaxLength(50);
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ConceptCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.TaxAmount).HasPrecision(18, 2);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.PaymentCurrency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.PaymentAmount).HasPrecision(18, 2);
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 6);
        builder.Property(e => e.RateSource).HasMaxLength(30);
        builder.Property(e => e.BillingTaxId).HasMaxLength(20).IsRequired();
        builder.Property(e => e.BillingName).HasMaxLength(200).IsRequired();

        // Un mismo cargo una sola vez por carro (M5-01).
        builder.HasIndex(e => new { e.CartId, e.ItemType, e.SourceId }).IsUnique();
        builder.HasIndex(e => e.LockedByPaymentId);
    }
}

internal sealed class PaymentStatusChangeConfiguration : IEntityTypeConfiguration<PaymentStatusChange>
{
    public void Configure(EntityTypeBuilder<PaymentStatusChange> builder)
    {
        builder.ToTable("PaymentStatusChanges");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FromStatus).HasMaxLength(30);
        builder.Property(e => e.ToStatus).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ChangedBy).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(500);

        builder.HasIndex(e => new { e.PaymentId, e.ChangedAt });

        builder.HasOne(e => e.Payment)
            .WithMany(p => p.StatusHistory)
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PaymentOutboxMessageConfiguration : IEntityTypeConfiguration<PaymentOutboxMessage>
{
    public void Configure(EntityTypeBuilder<PaymentOutboxMessage> builder)
    {
        builder.ToTable("PaymentOutboxMessages");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(1000);

        builder.HasIndex(e => new { e.Status, e.NextAttemptAt });
        builder.HasIndex(e => e.PaymentId);

        builder.HasOne(e => e.Payment)
            .WithMany()
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PaymentCurrencyRuleConfiguration : IEntityTypeConfiguration<PaymentCurrencyRule>
{
    public void Configure(EntityTypeBuilder<PaymentCurrencyRule> builder)
    {
        builder.ToTable("PaymentCurrencyRules");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ConceptCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(5).IsRequired();

        builder.HasIndex(e => new { e.Country, e.ConceptCode, e.Currency })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}

internal sealed class PaymentMethodConfigConfiguration : IEntityTypeConfiguration<PaymentMethodConfig>
{
    public void Configure(EntityTypeBuilder<PaymentMethodConfig> builder)
    {
        builder.ToTable("PaymentMethodConfigs");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Code).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(300);
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Kind).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ProviderKey).HasMaxLength(30);
        builder.Property(e => e.Currencies).HasMaxLength(100).IsRequired();

        builder.HasIndex(e => new { e.Country, e.Code })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}

internal sealed class PaymentBlockWindowConfiguration : IEntityTypeConfiguration<PaymentBlockWindow>
{
    public void Configure(EntityTypeBuilder<PaymentBlockWindow> builder)
    {
        builder.ToTable("PaymentBlockWindows");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Country).HasMaxLength(5);
        builder.Property(e => e.Reason).HasMaxLength(300).IsRequired();
        builder.Property(e => e.ClientMessage).HasMaxLength(500).IsRequired();

        builder.HasIndex(e => new { e.IsActive, e.EndDate });
    }
}

internal sealed class CustomerInvoiceConfiguration : IEntityTypeConfiguration<CustomerInvoice>
{
    public void Configure(EntityTypeBuilder<CustomerInvoice> builder)
    {
        builder.ToTable("CustomerInvoices");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.SiiNumber).HasMaxLength(30);
        builder.Property(e => e.SourceNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DocumentType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.BlNumber).HasMaxLength(50);
        builder.Property(e => e.BookingNumber).HasMaxLength(50);
        builder.Property(e => e.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TaxId).HasMaxLength(20).IsRequired();
        builder.Property(e => e.NetAmount).HasPrecision(18, 2);
        builder.Property(e => e.TaxAmount).HasPrecision(18, 2);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.SiiStatus).HasMaxLength(30);
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ConceptCode).HasMaxLength(50);
        builder.Property(e => e.Source).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => new { e.OrganizationId, e.IssueDate });
        builder.HasIndex(e => new { e.OrganizationId, e.SourceNumber })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(e => e.BillOfLadingId);

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
