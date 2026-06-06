using HapagPortal.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    private const int AuditFieldMaxLength = 256;

    public static void ConfigureBaseAuditableEntity<T>(this EntityTypeBuilder<T> builder)
        where T : BaseAuditableEntity
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(AuditFieldMaxLength)
            .IsRequired();

        builder.Property(e => e.ModifiedAt);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(AuditFieldMaxLength);

        builder.Property(e => e.DeletedAt);

        builder.Property(e => e.DeletedBy)
            .HasMaxLength(AuditFieldMaxLength);

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
