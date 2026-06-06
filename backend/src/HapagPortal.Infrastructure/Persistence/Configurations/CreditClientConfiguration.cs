using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class CreditClientConfiguration : IEntityTypeConfiguration<CreditClient>
{
    public void Configure(EntityTypeBuilder<CreditClient> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("CreditClients");

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.CreditLimit)
            .HasPrecision(18, 2);

        builder.Property(e => e.CreditStatus)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.ApprovedBy)
            .HasMaxLength(256);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ClientId, e.Country })
            .IsUnique();
    }
}
