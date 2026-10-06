using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ShipmentDocumentConfiguration : IEntityTypeConfiguration<ShipmentDocument>
{
    public void Configure(EntityTypeBuilder<ShipmentDocument> builder)
    {
        builder.ToTable("ShipmentDocuments");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.DocumentType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DocumentNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.BlNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.BookingNumber).HasMaxLength(50);
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ContainerNumbers).HasMaxLength(2000);
        builder.Property(e => e.IssuedByEmail).HasMaxLength(256);
        builder.Property(e => e.Origin).HasMaxLength(20).IsRequired();
        builder.Property(e => e.GenerationKey).HasMaxLength(150);
        builder.Property(e => e.StorageKey).HasMaxLength(200);
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ContentHash).HasMaxLength(64);
        builder.Property(e => e.VerificationCode).HasMaxLength(30).IsRequired();
        builder.Property(e => e.SignatureId).HasMaxLength(100);
        builder.Property(e => e.SignatureProvider).HasMaxLength(50);
        builder.Property(e => e.SignatureLevel).HasMaxLength(20);
        builder.Property(e => e.TemplateJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.RecipientEmails).HasMaxLength(1000);
        builder.Property(e => e.TermsVersion).HasMaxLength(50);

        builder.HasIndex(e => e.DocumentNumber).IsUnique();

        // Idempotencia de la emisión automática tras el pago (NF-03): un ítem pagado, un documento.
        builder.HasIndex(e => e.GenerationKey)
            .IsUnique()
            .HasFilter("\"GenerationKey\" IS NOT NULL");

        builder.HasIndex(e => new { e.BillOfLadingId, e.DocumentType, e.IssuedAt });
        builder.HasIndex(e => new { e.IssuedForOrganizationId, e.DocumentType, e.Status });
        builder.HasIndex(e => e.PaymentId);

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ShipmentDocumentEventConfiguration : IEntityTypeConfiguration<ShipmentDocumentEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentDocumentEvent> builder)
    {
        builder.ToTable("ShipmentDocumentEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Channel).HasMaxLength(20).IsRequired();
        builder.Property(e => e.UserEmail).HasMaxLength(256);
        builder.Property(e => e.Recipient).HasMaxLength(256);
        builder.Property(e => e.Details).HasMaxLength(2000);

        builder.HasIndex(e => new { e.ShipmentDocumentId, e.OccurredAt });
        builder.HasIndex(e => new { e.OrganizationId, e.OccurredAt });

        // Registro de NF-14/NF-16: se conserva con el documento; nunca se borra en cascada.
        builder.HasOne(e => e.ShipmentDocument)
            .WithMany(d => d.Events)
            .HasForeignKey(e => e.ShipmentDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
