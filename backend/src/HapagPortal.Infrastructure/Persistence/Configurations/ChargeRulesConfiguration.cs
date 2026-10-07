using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ChargeConceptConfiguration : IEntityTypeConfiguration<ChargeConcept>
{
    public void Configure(EntityTypeBuilder<ChargeConcept> builder)
    {
        builder.ToTable("ChargeConcepts");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Countries).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
    }
}

internal sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.ToTable("Tariffs");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.ConceptCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(20);
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ContainerType).HasMaxLength(10);
        builder.Property(e => e.Description).HasMaxLength(300);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.TierUnit).HasMaxLength(20).IsRequired();
        builder.Property(e => e.TierMode).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => new { e.ConceptCode, e.Country, e.IsActive, e.ValidFrom });

        builder.HasMany(e => e.Tiers)
            .WithOne(t => t.Tariff)
            .HasForeignKey(t => t.TariffId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TariffTierConfiguration : IEntityTypeConfiguration<TariffTier>
{
    public void Configure(EntityTypeBuilder<TariffTier> builder)
    {
        builder.ToTable("TariffTiers");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 2);

        builder.HasIndex(e => new { e.TariffId, e.FromUnit });
    }
}

internal sealed class MaintainerChangeLogConfiguration : IEntityTypeConfiguration<MaintainerChangeLog>
{
    public void Configure(EntityTypeBuilder<MaintainerChangeLog> builder)
    {
        builder.ToTable("MaintainerChangeLogs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Maintainer).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(20).IsRequired();
        builder.Property(e => e.PreviousValue).HasColumnType("text");
        builder.Property(e => e.NewValue).HasColumnType("text");
        builder.Property(e => e.ChangedBy).HasMaxLength(256).IsRequired();

        builder.HasIndex(e => new { e.Maintainer, e.EntityId, e.ChangedAt });
        builder.HasIndex(e => e.ChangedAt);
    }
}

internal sealed class InternalChargeRuleConfiguration : IEntityTypeConfiguration<InternalChargeRule>
{
    public void Configure(EntityTypeBuilder<InternalChargeRule> builder)
    {
        builder.ToTable("InternalChargeRules");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.RuleType).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.TaxId).HasMaxLength(20);
        builder.Property(e => e.MatchCode).HasMaxLength(20);
        builder.Property(e => e.AccountName).HasMaxLength(200);
        builder.Property(e => e.Reason).HasMaxLength(500);

        builder.HasIndex(e => new { e.RuleType, e.Country, e.IsActive });
    }
}

internal sealed class AppliedExemptionConfiguration : IEntityTypeConfiguration<AppliedExemption>
{
    public void Configure(EntityTypeBuilder<AppliedExemption> builder)
    {
        builder.ToTable("AppliedExemptions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ConceptCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ExemptParty).HasMaxLength(30).IsRequired();
        builder.Property(e => e.PartyTaxId).HasMaxLength(20).IsRequired();
        builder.Property(e => e.PartyMatchCode).HasMaxLength(20);
        builder.Property(e => e.ExemptAmount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ConditionAmount).HasPrecision(18, 2);
        builder.Property(e => e.ConditionCurrency).HasMaxLength(5);
        builder.Property(e => e.Source).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => e.BillOfLadingId);
        builder.HasIndex(e => e.LocalChargeId);

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExchangeRateRecordConfiguration : IEntityTypeConfiguration<ExchangeRateRecord>
{
    public void Configure(EntityTypeBuilder<ExchangeRateRecord> builder)
    {
        builder.ToTable("ExchangeRateRecords");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TransactionType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.FromCurrency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ToCurrency).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Rate).HasPrecision(18, 6);
        builder.Property(e => e.Source).HasMaxLength(30).IsRequired();
        builder.Property(e => e.SourceAmount).HasPrecision(18, 2);
        builder.Property(e => e.ConvertedAmount).HasPrecision(18, 2);

        builder.HasIndex(e => new { e.TransactionType, e.TransactionId });
    }
}

internal sealed class WarehouseChangeBatchConfiguration : IEntityTypeConfiguration<WarehouseChangeBatch>
{
    public void Configure(EntityTypeBuilder<WarehouseChangeBatch> builder)
    {
        builder.ToTable("WarehouseChangeBatches");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Status).HasMaxLength(30).IsRequired();

        builder.HasIndex(e => new { e.Status, e.CreatedAt });
        builder.HasIndex(e => e.ClientId);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.Batch)
            .HasForeignKey(i => i.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WarehouseChangeBatchItemConfiguration : IEntityTypeConfiguration<WarehouseChangeBatchItem>
{
    public void Configure(EntityTypeBuilder<WarehouseChangeBatchItem> builder)
    {
        builder.ToTable("WarehouseChangeBatchItems");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BlNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ContainerNumber).HasMaxLength(20);
        builder.Property(e => e.FromWarehouse).HasMaxLength(100);
        builder.Property(e => e.ToWarehouse).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TariffCode).HasMaxLength(20);
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(e => new { e.BatchId, e.Status, e.LineNumber });
    }
}

internal sealed class BusinessHolidayConfiguration : IEntityTypeConfiguration<BusinessHoliday>
{
    public void Configure(EntityTypeBuilder<BusinessHoliday> builder)
    {
        builder.ToTable("BusinessHolidays");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();

        builder.HasIndex(e => new { e.Country, e.Date }).IsUnique();
    }
}
