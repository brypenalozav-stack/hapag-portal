using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ReleaseLetterRequestConfiguration : IEntityTypeConfiguration<ReleaseLetterRequest>
{
    public void Configure(EntityTypeBuilder<ReleaseLetterRequest> builder)
    {
        builder.ToTable("ReleaseLetterRequests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LegalEntityType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.TatcStatusAtSubmission).HasMaxLength(20);
        builder.Property(e => e.TatcErrorAtSubmission).HasMaxLength(100);
        builder.Property(e => e.TatcSnapshotAtSubmission).HasColumnType("text");
        builder.Property(e => e.TatcStatusAtApproval).HasMaxLength(20);
        builder.Property(e => e.TatcErrorAtApproval).HasMaxLength(100);
        builder.Property(e => e.TatcSnapshotAtApproval).HasColumnType("text");

        builder.HasIndex(e => e.ServiceRequestId).IsUnique();

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(e => e.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AssistantDocumentDeliveryConfiguration : IEntityTypeConfiguration<AssistantDocumentDelivery>
{
    public void Configure(EntityTypeBuilder<AssistantDocumentDelivery> builder)
    {
        builder.ToTable("AssistantDocumentDeliveries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserEmail).HasMaxLength(256);
        builder.Property(e => e.BlNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DocumentKind).HasMaxLength(30).IsRequired();
        builder.Property(e => e.DocumentType).HasMaxLength(50);
        builder.Property(e => e.DocumentNumber).HasMaxLength(50).IsRequired();

        builder.HasIndex(e => new { e.SessionId, e.DeliveredAt });
        builder.HasIndex(e => new { e.UserId, e.DeliveredAt });
        builder.HasIndex(e => new { e.DocumentId, e.DeliveredAt });

        // Registro de NF-14/NF-16: se conserva con la conversación; nunca se borra en cascada.
        builder.HasOne<AssistantSession>()
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
