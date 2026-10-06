using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ShipmentRoleConfiguration : IEntityTypeConfiguration<ShipmentRole>
{
    public void Configure(EntityTypeBuilder<ShipmentRole> builder)
    {
        builder.ToTable("ShipmentRoles");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Role).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Source).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => new { e.BillOfLadingId, e.ClientId, e.Role }).IsUnique();
        builder.HasIndex(e => e.ClientId);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ShipmentActionConfiguration : IEntityTypeConfiguration<ShipmentAction>
{
    public void Configure(EntityTypeBuilder<ShipmentAction> builder)
    {
        builder.ToTable("ShipmentActions");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Kind).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Scope).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasMany(e => e.Rules)
            .WithOne(r => r.ShipmentAction)
            .HasForeignKey(r => r.ShipmentActionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ShipmentAccessRuleConfiguration : IEntityTypeConfiguration<ShipmentAccessRule>
{
    public void Configure(EntityTypeBuilder<ShipmentAccessRule> builder)
    {
        builder.ToTable("ShipmentAccessRules");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Role).HasMaxLength(30).IsRequired();
        builder.Property(e => e.OrganizationType).HasMaxLength(30);
        builder.Property(e => e.Level).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => new { e.ShipmentActionId, e.Role, e.OrganizationType }).IsUnique();
    }
}

internal sealed class OrganizationDocumentConfiguration : IEntityTypeConfiguration<OrganizationDocument>
{
    public void Configure(EntityTypeBuilder<OrganizationDocument> builder)
    {
        builder.ToTable("OrganizationDocuments");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.DocumentType).HasMaxLength(40).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.StorageKey).HasMaxLength(300).IsRequired();

        builder.HasIndex(e => e.ClientId);
    }
}
