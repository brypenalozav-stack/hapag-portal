using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
{
    public void Configure(EntityTypeBuilder<ApiClient> builder)
    {
        builder.ToTable("ApiClients");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Scopes).HasMaxLength(200).IsRequired();
        builder.Property(e => e.SignatoryName).HasMaxLength(200);
        builder.Property(e => e.SignatoryTaxId).HasMaxLength(30);
        builder.Property(e => e.SignatoryPosition).HasMaxLength(100);
        builder.Property(e => e.SignatoryEmail).HasMaxLength(256);
        builder.Property(e => e.TechnicalContactEmail).HasMaxLength(256);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.RevokedBy).HasMaxLength(256);
        builder.Property(e => e.RevocationReason).HasMaxLength(500);

        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.TechnicalUserId).IsUnique();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.TechnicalUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ApiClientKeyConfiguration : IEntityTypeConfiguration<ApiClientKey>
{
    public void Configure(EntityTypeBuilder<ApiClientKey> builder)
    {
        builder.ToTable("ApiClientKeys");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(e => e.KeyHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(e => e.RevokedBy).HasMaxLength(256);

        // La clave se ubica por su prefijo público; solo se guarda el SHA-256 (NF-09).
        builder.HasIndex(e => e.Prefix).IsUnique();
        builder.HasIndex(e => e.ApiClientId);

        builder.HasOne(e => e.ApiClient)
            .WithMany(c => c.Keys)
            .HasForeignKey(e => e.ApiClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ApiClientRequestConfiguration : IEntityTypeConfiguration<ApiClientRequest>
{
    public void Configure(EntityTypeBuilder<ApiClientRequest> builder)
    {
        builder.ToTable("ApiClientRequests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Operation).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Method).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Path).HasMaxLength(300).IsRequired();
        builder.Property(e => e.IdempotencyKey).HasMaxLength(100);
        builder.Property(e => e.RequestHash).HasMaxLength(64);
        builder.Property(e => e.Outcome).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.ResponseJson).HasColumnType("text");
        builder.Property(e => e.TargetType).HasMaxLength(40);
        builder.Property(e => e.TargetReference).HasMaxLength(100);
        builder.Property(e => e.BlNumber).HasMaxLength(50);
        builder.Property(e => e.SourceAddress).HasMaxLength(64);

        // Idempotencia por cliente (M3-17): una clave, una solicitud.
        builder.HasIndex(e => new { e.ApiClientId, e.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        // Límite por cliente y listado de sus solicitudes.
        builder.HasIndex(e => new { e.ApiClientId, e.ReceivedAt });
        builder.HasIndex(e => new { e.OrganizationId, e.ReceivedAt });

        builder.HasOne<ApiClient>()
            .WithMany()
            .HasForeignKey(e => e.ApiClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
