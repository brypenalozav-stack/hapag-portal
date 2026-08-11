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

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.RoleCode);
        builder.HasIndex(e => e.DedupKey);
    }
}
