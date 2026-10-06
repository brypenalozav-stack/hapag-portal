using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ShipmentPublicationRuleConfiguration : IEntityTypeConfiguration<ShipmentPublicationRule>
{
    public void Configure(EntityTypeBuilder<ShipmentPublicationRule> builder)
    {
        builder.ToTable("ShipmentPublicationRules");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.FinalDestinationCode).HasMaxLength(10).IsRequired();
        builder.Property(e => e.FinalDestinationName).HasMaxLength(100);
        builder.Property(e => e.DischargePortCode).HasMaxLength(10);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => new { e.Country, e.FinalDestinationCode, e.IsActive });
    }
}

internal sealed class TatcBatchConfiguration : IEntityTypeConfiguration<TatcBatch>
{
    public void Configure(EntityTypeBuilder<TatcBatch> builder)
    {
        builder.ToTable("TatcBatches");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequestedByEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.LocationCode).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(30).IsRequired();
        builder.Property(e => e.SourceRequestId).HasMaxLength(100);
        builder.Property(e => e.ErrorCode).HasMaxLength(100);

        builder.HasIndex(e => new { e.ClientId, e.CreatedAt });

        builder.HasMany(e => e.Items)
            .WithOne(i => i.Batch)
            .HasForeignKey(i => i.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TatcBatchItemConfiguration : IEntityTypeConfiguration<TatcBatchItem>
{
    public void Configure(EntityTypeBuilder<TatcBatchItem> builder)
    {
        builder.ToTable("TatcBatchItems");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BlNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ReasonCode).HasMaxLength(50);

        builder.HasIndex(e => new { e.BatchId, e.LineNumber });
    }
}

internal sealed class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.ToTable("KnowledgeArticles");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Topic).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Content).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Keywords).HasMaxLength(500);

        builder.HasIndex(e => new { e.Country, e.IsActive, e.Topic });
    }
}

internal sealed class AssistantMailboxConfiguration : IEntityTypeConfiguration<AssistantMailbox>
{
    public void Configure(EntityTypeBuilder<AssistantMailbox> builder)
    {
        builder.ToTable("AssistantMailboxes");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Topic).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(300);

        builder.HasIndex(e => new { e.Country, e.Topic }).IsUnique();
    }
}

internal sealed class AssistantSessionConfiguration : IEntityTypeConfiguration<AssistantSession>
{
    public void Configure(EntityTypeBuilder<AssistantSession> builder)
    {
        builder.ToTable("AssistantSessions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Country).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Language).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EngineMode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.TranscriptSentTo).HasMaxLength(256);

        builder.HasIndex(e => new { e.UserId, e.StartedAt });

        builder.HasMany(e => e.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AssistantMessageConfiguration : IEntityTypeConfiguration<AssistantMessage>
{
    public void Configure(EntityTypeBuilder<AssistantMessage> builder)
    {
        builder.ToTable("AssistantMessages");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Role).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Content).HasColumnType("text").IsRequired();
        builder.Property(e => e.Intent).HasMaxLength(30);
        builder.Property(e => e.AnswerType).HasMaxLength(30);
        builder.Property(e => e.CitationsJson).HasColumnType("text");
        builder.Property(e => e.ActionsJson).HasColumnType("text");
        builder.Property(e => e.Engine).HasMaxLength(20);

        builder.HasIndex(e => new { e.SessionId, e.Sequence }).IsUnique();
        builder.HasIndex(e => new { e.Role, e.CreatedAt });
    }
}

internal sealed class DangerousGoodConfiguration : IEntityTypeConfiguration<DangerousGood>
{
    public void Configure(EntityTypeBuilder<DangerousGood> builder)
    {
        builder.ToTable("DangerousGoods");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.UnNumber).HasMaxLength(4);
        builder.Property(e => e.ProperShippingNameEs).HasMaxLength(300).IsRequired();
        builder.Property(e => e.ProperShippingNameEn).HasMaxLength(300).IsRequired();
        builder.Property(e => e.HazardClass).HasMaxLength(5);
        builder.Property(e => e.SubsidiaryRisk).HasMaxLength(20);
        builder.Property(e => e.PackingGroup).HasMaxLength(3);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.Keywords).HasMaxLength(500);
        builder.Property(e => e.Source).HasMaxLength(20).IsRequired();
        builder.Property(e => e.SearchText).HasMaxLength(1500).IsRequired();

        builder.HasIndex(e => e.UnNumber);
    }
}
