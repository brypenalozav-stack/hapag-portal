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

        builder.HasIndex(e => e.PaymentNumber)
            .IsUnique();

        builder.HasMany(e => e.Details)
            .WithOne(e => e.Payment)
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
