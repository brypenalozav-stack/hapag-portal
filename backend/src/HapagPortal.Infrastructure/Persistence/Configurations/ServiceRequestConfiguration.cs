using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ServiceDefinitionConfiguration : IEntityTypeConfiguration<ServiceDefinition>
{
    public void Configure(EntityTypeBuilder<ServiceDefinition> builder)
    {
        builder.ToTable("ServiceDefinitions");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.NameEs).HasMaxLength(150).IsRequired();
        builder.Property(e => e.NameEn).HasMaxLength(150).IsRequired();
        builder.Property(e => e.DescriptionEs).HasMaxLength(1000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(1000);
        builder.Property(e => e.Operations).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Countries).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ReferenceType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.RequiredBlStatuses).HasMaxLength(300);
        builder.Property(e => e.AvailabilityWindow).HasMaxLength(30).IsRequired();
        builder.Property(e => e.InputSchemaJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.PricingMode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ChargeConceptCode).HasMaxLength(50);
        builder.Property(e => e.TariffCode).HasMaxLength(20);
        builder.Property(e => e.LateTariffCode).HasMaxLength(20);
        builder.Property(e => e.QuantityMode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.MeasureFieldKey).HasMaxLength(40);
        builder.Property(e => e.Milestone).HasMaxLength(30).IsRequired();
        builder.Property(e => e.DeadlineRuleCode).HasMaxLength(50);
        builder.Property(e => e.TimingRule).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ExemptionConcept).HasMaxLength(50);
        builder.Property(e => e.ApprovalTeam).HasMaxLength(30).IsRequired();
        builder.Property(e => e.FulfillmentTeam).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ActionCode).HasMaxLength(60).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => new { e.IsActive, e.DisplayOrder });
    }
}

internal sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.RequestNumber).HasMaxLength(30).IsRequired();
        builder.Property(e => e.DefinitionCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RequestedByEmail).HasMaxLength(256);
        builder.Property(e => e.BlNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.BookingNumber).HasMaxLength(50);
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Operation).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ContainerNumbers).HasMaxLength(2000);
        builder.Property(e => e.InputValuesJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.BillingTaxId).HasMaxLength(20);
        builder.Property(e => e.BillingName).HasMaxLength(200);
        builder.Property(e => e.BillingAddress).HasMaxLength(300);
        builder.Property(e => e.BillingEmail).HasMaxLength(256);
        builder.Property(e => e.BillingActivity).HasMaxLength(200);
        builder.Property(e => e.ChargeConceptCode).HasMaxLength(50);
        builder.Property(e => e.TariffCode).HasMaxLength(20);
        builder.Property(e => e.TariffSource).HasMaxLength(20);
        builder.Property(e => e.TierUnit).HasMaxLength(20);
        builder.Property(e => e.Timing).HasMaxLength(20);
        builder.Property(e => e.MilestoneSource).HasMaxLength(30);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.TaxAmount).HasPrecision(18, 2);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(5);
        builder.Property(e => e.PricingDetailJson).HasColumnType("text");
        builder.Property(e => e.ExemptionReference).HasMaxLength(200);
        builder.Property(e => e.Status).HasMaxLength(30).IsRequired();
        builder.Property(e => e.AssignedTeam).HasMaxLength(30);
        builder.Property(e => e.ResolutionNotes).HasMaxLength(1000);

        builder.HasIndex(e => e.RequestNumber).IsUnique();
        builder.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
        builder.HasIndex(e => new { e.Status, e.AssignedTeam });
        builder.HasIndex(e => new { e.BillOfLadingId, e.DefinitionId });

        builder.HasOne(e => e.Definition)
            .WithMany()
            .HasForeignKey(e => e.DefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Events)
            .WithOne(e => e.ServiceRequest)
            .HasForeignKey(e => e.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
            .WithOne(a => a.ServiceRequest)
            .HasForeignKey(a => a.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Charges)
            .WithOne(c => c.ServiceRequest)
            .HasForeignKey(c => c.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ServiceRequestEventConfiguration : IEntityTypeConfiguration<ServiceRequestEvent>
{
    public void Configure(EntityTypeBuilder<ServiceRequestEvent> builder)
    {
        builder.ToTable("ServiceRequestEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FromStatus).HasMaxLength(30);
        builder.Property(e => e.ToStatus).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ActorName).HasMaxLength(256).IsRequired();
        builder.Property(e => e.ActorKind).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => new { e.ServiceRequestId, e.OccurredAt, e.Sequence });
    }
}

internal sealed class ServiceRequestAttachmentConfiguration : IEntityTypeConfiguration<ServiceRequestAttachment>
{
    public void Configure(EntityTypeBuilder<ServiceRequestAttachment> builder)
    {
        builder.ToTable("ServiceRequestAttachments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FieldKey).HasMaxLength(40).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(e => e.UploadedBy).HasMaxLength(256).IsRequired();

        builder.HasIndex(e => new { e.ServiceRequestId, e.FieldKey });
    }
}

internal sealed class ServiceRequestChargeConfiguration : IEntityTypeConfiguration<ServiceRequestCharge>
{
    public void Configure(EntityTypeBuilder<ServiceRequestCharge> builder)
    {
        builder.ToTable("ServiceRequestCharges");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.LocalChargeId);
        builder.HasIndex(e => new { e.ServiceRequestId, e.LocalChargeId }).IsUnique();
    }
}
