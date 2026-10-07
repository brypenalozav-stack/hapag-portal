using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Type).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.RoleCode).HasMaxLength(40);
        builder.Property(e => e.DedupKey).HasMaxLength(200);
        builder.Property(e => e.Module).HasMaxLength(30);
        builder.Property(e => e.EntityType).HasMaxLength(40);
        builder.Property(e => e.EntityId).HasMaxLength(64);
        builder.Property(e => e.EntityReference).HasMaxLength(200);
        builder.Property(e => e.BlNumber).HasMaxLength(50);
        builder.Property(e => e.ActionType).HasMaxLength(40);
        builder.Property(e => e.ActionTargetId).HasMaxLength(64);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.ActionType, e.ActionTargetId });
        builder.HasIndex(e => e.RoleCode);
        builder.HasIndex(e => e.DedupKey);
    }
}
