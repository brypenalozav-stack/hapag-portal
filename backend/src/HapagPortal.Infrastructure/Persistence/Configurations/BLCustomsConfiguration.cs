using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class BLPartyConfiguration : IEntityTypeConfiguration<BLParty>
{
    public void Configure(EntityTypeBuilder<BLParty> builder)
    {
        builder.ToTable("BLParties");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Role).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(250).IsRequired();
        builder.Property(e => e.TaxId).HasMaxLength(50);
        builder.Property(e => e.TaxIdType).HasMaxLength(20);
        builder.Property(e => e.CountryCode).HasMaxLength(5);
        builder.Property(e => e.Address).HasMaxLength(500);

        builder.HasOne(e => e.BillOfLading)
            .WithMany(b => b.Parties)
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BLCargoItemConfiguration : IEntityTypeConfiguration<BLCargoItem>
{
    public void Configure(EntityTypeBuilder<BLCargoItem> builder)
    {
        builder.ToTable("BLCargoItems");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.HsCode).HasMaxLength(20);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.PackageCode).HasMaxLength(10);
        builder.Property(e => e.ShippingMarks).HasMaxLength(1000);
        builder.Property(e => e.GrossWeight).HasPrecision(18, 3);
        builder.Property(e => e.NetWeight).HasPrecision(18, 3);
        builder.Property(e => e.Volume).HasPrecision(18, 3);

        builder.HasOne(e => e.BillOfLading)
            .WithMany(b => b.CargoItems)
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
