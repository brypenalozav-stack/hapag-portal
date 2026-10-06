using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.NotificationType).HasMaxLength(40).IsRequired();

        // Una preferencia por usuario y tipo (M1-25).
        builder.HasIndex(e => new { e.UserId, e.NotificationType }).IsUnique();
    }
}

internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.TitleEs).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TitleEn).HasMaxLength(200).IsRequired();
        builder.Property(e => e.BodyEs).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.BodyEn).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Countries).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Operation).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Severity).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.PublishedBy).HasMaxLength(256);
        builder.Property(e => e.UnpublishedBy).HasMaxLength(256);

        builder.HasIndex(e => new { e.Status, e.ValidFrom });
    }
}

internal sealed class GuideDefinitionConfiguration : IEntityTypeConfiguration<GuideDefinition>
{
    public void Configure(EntityTypeBuilder<GuideDefinition> builder)
    {
        builder.ToTable("GuideDefinitions");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Code).HasMaxLength(60).IsRequired();
        builder.Property(e => e.NameEs).HasMaxLength(150).IsRequired();
        builder.Property(e => e.NameEn).HasMaxLength(150).IsRequired();
        builder.Property(e => e.DescriptionEs).HasMaxLength(500);
        builder.Property(e => e.DescriptionEn).HasMaxLength(500);
        builder.Property(e => e.Route).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Audience).HasMaxLength(20).IsRequired();
        builder.Property(e => e.StepsJson).HasColumnType("text").IsRequired();

        builder.HasIndex(e => e.Code).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}

internal sealed class UserGuideStateConfiguration : IEntityTypeConfiguration<UserGuideState>
{
    public void Configure(EntityTypeBuilder<UserGuideState> builder)
    {
        builder.ToTable("UserGuideStates");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.GuideCode).HasMaxLength(60).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => new { e.UserId, e.GuideCode }).IsUnique();
    }
}

internal sealed class ImpersonationSessionConfiguration : IEntityTypeConfiguration<ImpersonationSession>
{
    public void Configure(EntityTypeBuilder<ImpersonationSession> builder)
    {
        builder.ToTable("ImpersonationSessions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ActorEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.SubjectEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.OrganizationName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EndReason).HasMaxLength(20);
        builder.Property(e => e.SourceAddress).HasMaxLength(64);

        builder.HasIndex(e => new { e.ActorUserId, e.Status });
        builder.HasIndex(e => new { e.OrganizationId, e.StartedAt });
        builder.HasIndex(e => e.StartedAt);
    }
}

internal sealed class CounterRecordConfiguration : IEntityTypeConfiguration<CounterRecord>
{
    public void Configure(EntityTypeBuilder<CounterRecord> builder)
    {
        builder.ToTable("CounterRecords");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.BlNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.SyncStatus).HasMaxLength(20).IsRequired();
        builder.Property(e => e.SyncError).HasMaxLength(100);
        builder.Property(e => e.SourceReference).HasMaxLength(100);
        builder.Property(e => e.RecordedBy).HasMaxLength(256).IsRequired();

        // Un registro de Counter por BL (M8-09); su historia está en el registro de mantenedores (NF-15).
        builder.HasIndex(e => e.BillOfLadingId).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(e => e.BlNumber);
        builder.HasIndex(e => e.SyncStatus);

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContactListChangeConfiguration : IEntityTypeConfiguration<ContactListChange>
{
    public void Configure(EntityTypeBuilder<ContactListChange> builder)
    {
        builder.ToTable("ContactListChanges");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReportType).HasMaxLength(40).IsRequired();
        builder.Property(e => e.PreviousEmails).HasMaxLength(6000);
        builder.Property(e => e.NewEmails).HasMaxLength(6000).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.SourceReference).HasMaxLength(100);
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.ChangedBy).HasMaxLength(256).IsRequired();

        builder.HasIndex(e => new { e.OrganizationId, e.ChangedAt });
    }
}

internal sealed class CarrierPreRegistrationConfiguration : IEntityTypeConfiguration<CarrierPreRegistration>
{
    public void Configure(EntityTypeBuilder<CarrierPreRegistration> builder)
    {
        builder.ToTable("CarrierPreRegistrations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequestedBy).HasMaxLength(256).IsRequired();
        builder.Property(e => e.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TaxId).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();

        // Una pre-creación por cliente y transportista: varias organizaciones pueden pre-crear al mismo sin duplicarlo.
        builder.HasIndex(e => new { e.CarrierOrganizationId, e.RequestedByOrganizationId }).IsUnique();
        builder.HasIndex(e => e.RequestedByOrganizationId);
    }
}

internal sealed class OrganizationParentLinkConfiguration : IEntityTypeConfiguration<OrganizationParentLink>
{
    public void Configure(EntityTypeBuilder<OrganizationParentLink> builder)
    {
        builder.ToTable("OrganizationParentLinks");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.VisibilityChangedBy).HasMaxLength(256);
        builder.Property(e => e.RequestedBy).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.DecidedBy).HasMaxLength(256);
        builder.Property(e => e.DecisionNotes).HasMaxLength(500);
        builder.Property(e => e.EndedBy).HasMaxLength(256);

        // Un vínculo pendiente o activo por filial (M1-21).
        builder.HasIndex(e => e.OrganizationId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Pending', 'Active')");
        builder.HasIndex(e => new { e.ParentOrganizationId, e.Status });
    }
}
