using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("Clients");

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.TaxId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TaxIdType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Phone)
            .HasMaxLength(30);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.City)
            .HasMaxLength(100);

        builder.Property(e => e.ClientType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.AgentCode)
            .HasMaxLength(20);

        builder.HasIndex(e => new { e.TaxId, e.Country })
            .IsUnique();

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.HasMany(e => e.BillsOfLading)
            .WithOne(e => e.Client)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Payments)
            .WithOne(e => e.Client)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
