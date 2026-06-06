using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.EntityId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.UserId)
            .HasMaxLength(50);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.HasIndex(e => e.EntityName);
        builder.HasIndex(e => e.Timestamp);
    }
}
