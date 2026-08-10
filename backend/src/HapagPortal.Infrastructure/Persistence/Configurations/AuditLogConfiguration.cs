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

        // 'text' es el tipo correcto en PostgreSQL; 'nvarchar(max)' es de SQL Server
        // y rompería las migraciones sobre Npgsql (deuda D1 / BUG menor Fase 8).
        builder.Property(e => e.OldValues)
            .HasColumnType("text");

        builder.Property(e => e.NewValues)
            .HasColumnType("text");

        builder.Property(e => e.UserId)
            .HasMaxLength(50);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.HasIndex(e => e.EntityName);
        builder.HasIndex(e => e.Timestamp);
    }
}
