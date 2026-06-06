using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseChangeConfiguration : IEntityTypeConfiguration<WarehouseChange>
{
    public void Configure(EntityTypeBuilder<WarehouseChange> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("WarehouseChanges");

        builder.Property(e => e.FromWarehouse)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ToWarehouse)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
