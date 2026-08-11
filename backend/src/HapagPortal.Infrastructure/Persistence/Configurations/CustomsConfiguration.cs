using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class CustomsManifestConfiguration : IEntityTypeConfiguration<CustomsManifest>
{
    public void Configure(EntityTypeBuilder<CustomsManifest> builder)
    {
        builder.ToTable("CustomsManifests");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.VesselImo).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Voyage).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Port).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Direction).HasMaxLength(10).IsRequired();

        builder.HasIndex(e => new { e.VesselImo, e.Voyage, e.Direction });
    }
}

internal sealed class CustomsTransmissionConfiguration : IEntityTypeConfiguration<CustomsTransmission>
{
    public void Configure(EntityTypeBuilder<CustomsTransmission> builder)
    {
        builder.ToTable("CustomsTransmissions");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Stage).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Kind).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Reference).HasMaxLength(100);
        builder.Property(e => e.ResponseCode).HasMaxLength(50);
        builder.Property(e => e.ResponseMessage).HasMaxLength(1000);

        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Manifest)
            .WithMany(m => m.Transmissions)
            .HasForeignKey(e => e.ManifestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CustomsTransmissionEventConfiguration : IEntityTypeConfiguration<CustomsTransmissionEvent>
{
    public void Configure(EntityTypeBuilder<CustomsTransmissionEvent> builder)
    {
        builder.ToTable("CustomsTransmissionEvents");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ResponseCode).HasMaxLength(50);
        builder.Property(e => e.ResponseMessage).HasMaxLength(1000);

        builder.HasOne(e => e.Transmission)
            .WithMany(t => t.Events)
            .HasForeignKey(e => e.TransmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
