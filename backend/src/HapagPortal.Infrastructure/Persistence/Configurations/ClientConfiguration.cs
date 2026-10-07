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

        builder.Property(e => e.OrganizationType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.RegistrationStatus)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.MatchCode)
            .HasMaxLength(20);

        builder.Property(e => e.OperatingCountries)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ValidatedBy)
            .HasMaxLength(256);

        builder.Property(e => e.ArCheckedBy)
            .HasMaxLength(256);

        builder.Property(e => e.ArReference)
            .HasMaxLength(100);

        builder.Property(e => e.ReviewNotes)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.TaxId, e.Country })
            .IsUnique();

        builder.HasIndex(e => e.MatchCode)
            .IsUnique()
            .HasFilter("\"MatchCode\" IS NOT NULL");

        builder.HasIndex(e => e.RegistrationStatus);

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.HasMany(e => e.BillsOfLading)
            .WithOne(e => e.Client)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Documents)
            .WithOne(e => e.Client)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Payments)
            .WithOne(e => e.Client)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
