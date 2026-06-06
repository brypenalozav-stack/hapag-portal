using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class BLContainerConfiguration : IEntityTypeConfiguration<BLContainer>
{
    public void Configure(EntityTypeBuilder<BLContainer> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("BLContainers");

        builder.Property(e => e.ContainerNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ContainerType)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.SealNumber)
            .HasMaxLength(30);

        builder.Property(e => e.Weight)
            .HasPrecision(18, 2);

        builder.Property(e => e.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(e => e.ContainerNumber);
    }
}
