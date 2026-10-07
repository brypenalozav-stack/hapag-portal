using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class AccessGrantConfiguration : IEntityTypeConfiguration<AccessGrant>
{
    public void Configure(EntityTypeBuilder<AccessGrant> builder)
    {
        builder.ToTable("AccessGrants");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.GrantorRole).HasMaxLength(30).IsRequired();
        builder.Property(e => e.BookingNumber).HasMaxLength(50);
        builder.Property(e => e.GrantType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.IntendedRole).HasMaxLength(30);
        builder.Property(e => e.ActionCodes).HasMaxLength(4000);
        builder.Property(e => e.CeilingActionCodes).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.ValidityType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.TermsVersion).HasMaxLength(50);
        builder.Property(e => e.EndReason).HasMaxLength(20);

        // Un único acceso abierto por BL + otorgante + tercero (M1-12); el anticipado por booking se
        // reconcilia aparte (M1-20).
        builder.HasIndex(e => new { e.BillOfLadingId, e.GrantorClientId, e.GranteeClientId })
            .IsUnique()
            .HasFilter("\"Status\" IN ('Active', 'PendingAcceptance') AND \"BillOfLadingId\" IS NOT NULL AND \"GrantType\" <> 'EarlyBooking'");

        builder.HasIndex(e => new { e.GranteeClientId, e.Status, e.ValidTo });
        builder.HasIndex(e => new { e.GrantorClientId, e.Status });
        builder.HasIndex(e => new { e.Status, e.ValidTo });
        builder.HasIndex(e => e.BookingNumber);
        builder.HasIndex(e => e.ParentGrantId);

        builder.HasOne(e => e.Grantor)
            .WithMany()
            .HasForeignKey(e => e.GrantorClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Grantee)
            .WithMany()
            .HasForeignKey(e => e.GranteeClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ParentGrant)
            .WithMany()
            .HasForeignKey(e => e.ParentGrantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DefaultGranteeConfiguration : IEntityTypeConfiguration<DefaultGrantee>
{
    public void Configure(EntityTypeBuilder<DefaultGrantee> builder)
    {
        builder.ToTable("DefaultGrantees");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.ActionCodes).HasMaxLength(4000);

        builder.HasIndex(e => new { e.GrantorClientId, e.GranteeClientId })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");

        builder.HasOne(e => e.Grantor)
            .WithMany()
            .HasForeignKey(e => e.GrantorClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Grantee)
            .WithMany()
            .HasForeignKey(e => e.GranteeClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OpenAccessSettingConfiguration : IEntityTypeConfiguration<OpenAccessSetting>
{
    public void Configure(EntityTypeBuilder<OpenAccessSetting> builder)
    {
        builder.ToTable("OpenAccessSettings");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.ActionCodes).HasMaxLength(4000);

        builder.HasIndex(e => e.ClientId).IsUnique();

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ShipmentAssociationConfiguration : IEntityTypeConfiguration<ShipmentAssociation>
{
    public void Configure(EntityTypeBuilder<ShipmentAssociation> builder)
    {
        builder.ToTable("ShipmentAssociations");
        builder.ConfigureBaseAuditableEntity();

        builder.HasIndex(e => new { e.BillOfLadingId, e.ClientId }).IsUnique();
        builder.HasIndex(e => e.ClientId);

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VisibilityWideningConfiguration : IEntityTypeConfiguration<VisibilityWidening>
{
    public void Configure(EntityTypeBuilder<VisibilityWidening> builder)
    {
        builder.ToTable("VisibilityWidenings");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.GrantorRole).HasMaxLength(30).IsRequired();
        builder.Property(e => e.TargetRole).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ActionCode).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EndReason).HasMaxLength(20);

        builder.HasIndex(e => new { e.BillOfLadingId, e.TargetRole, e.Status });
        builder.HasIndex(e => e.OriginGrantId);

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Grantor)
            .WithMany()
            .HasForeignKey(e => e.GrantorClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Registro append-only (M1-23): sin claves foráneas para que nunca se borre en cascada.</summary>
internal sealed class AccessAuditEntryConfiguration : IEntityTypeConfiguration<AccessAuditEntry>
{
    public void Configure(EntityTypeBuilder<AccessAuditEntry> builder)
    {
        builder.ToTable("AccessAuditEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.BlNumber).HasMaxLength(50);
        builder.Property(e => e.BookingNumber).HasMaxLength(50);
        builder.Property(e => e.ActorEmail).HasMaxLength(256);
        builder.Property(e => e.Details).HasColumnType("text");
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => new { e.BillOfLadingId, e.OccurredAt });
        builder.HasIndex(e => e.BookingNumber);
        builder.HasIndex(e => e.GrantorClientId);
        builder.HasIndex(e => e.GranteeClientId);
        builder.HasIndex(e => e.AccessGrantId);
    }
}
