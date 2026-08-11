using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class DeadlineRuleConfiguration : IEntityTypeConfiguration<DeadlineRule>
{
    public void Configure(EntityTypeBuilder<DeadlineRule> builder)
    {
        builder.ToTable("DeadlineRules");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.BaseEvent).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Direction).HasMaxLength(10);
        builder.Property(e => e.Country).HasMaxLength(5);
        builder.Property(e => e.BLType).HasMaxLength(20);
        builder.Property(e => e.Severity).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Source).HasMaxLength(300);
        builder.Property(e => e.Certainty).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
    }
}

internal sealed class DeadlineInstanceConfiguration : IEntityTypeConfiguration<DeadlineInstance>
{
    public void Configure(EntityTypeBuilder<DeadlineInstance> builder)
    {
        builder.ToTable("DeadlineInstances");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => new { e.RuleId, e.ManifestId, e.BillOfLadingId });

        builder.HasOne(e => e.Rule)
            .WithMany(r => r.Instances)
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Manifest)
            .WithMany()
            .HasForeignKey(e => e.ManifestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
