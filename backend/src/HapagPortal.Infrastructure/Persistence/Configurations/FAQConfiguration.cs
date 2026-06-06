using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class FAQConfiguration : IEntityTypeConfiguration<FAQ>
{
    public void Configure(EntityTypeBuilder<FAQ> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("FAQs");

        builder.Property(e => e.Question)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Answer)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.HasIndex(e => new { e.Country, e.Category });
    }
}
