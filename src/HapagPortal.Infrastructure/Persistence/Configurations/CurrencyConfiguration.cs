using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("Currencies");

        builder.Property(e => e.Code)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Symbol)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.ExchangeRateToUSD)
            .HasPrecision(18, 6);

        builder.HasIndex(e => e.Code)
            .IsUnique();
    }
}
