using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class BillOfLadingConfiguration : IEntityTypeConfiguration<BillOfLading>
{
    public void Configure(EntityTypeBuilder<BillOfLading> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("BillsOfLading");

        builder.Property(e => e.BLNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ShipmentType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Vessel)
            .HasMaxLength(100);

        builder.Property(e => e.Voyage)
            .HasMaxLength(50);

        builder.Property(e => e.PortOfLoading)
            .HasMaxLength(100);

        builder.Property(e => e.PortOfDischarge)
            .HasMaxLength(100);

        builder.Property(e => e.Consignee)
            .HasMaxLength(200);

        builder.Property(e => e.Shipper)
            .HasMaxLength(200);

        builder.Property(e => e.NotifyParty)
            .HasMaxLength(200);

        builder.Property(e => e.FreightAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.FreightCurrency)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.HasIndex(e => e.BLNumber)
            .IsUnique();

        builder.HasMany(e => e.Containers)
            .WithOne(e => e.BillOfLading)
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.LocalCharges)
            .WithOne(e => e.BillOfLading)
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.DemurrageCharges)
            .WithOne(e => e.BillOfLading)
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Payments)
            .WithOne(e => e.BillOfLading)
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
