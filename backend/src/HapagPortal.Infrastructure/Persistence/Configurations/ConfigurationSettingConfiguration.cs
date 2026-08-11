using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ConfigurationSettingConfiguration : IEntityTypeConfiguration<ConfigurationSetting>
{
    public void Configure(EntityTypeBuilder<ConfigurationSetting> builder)
    {
        builder.ToTable("ConfigurationSettings");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Scope).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Key).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Value).HasColumnType("text");

        // Un valor por (ámbito, cliente, clave). ClientId nulo para ámbito Global.
        builder.HasIndex(e => new { e.Scope, e.ClientId, e.Key }).IsUnique();
    }
}

internal sealed class SecretCredentialConfiguration : IEntityTypeConfiguration<SecretCredential>
{
    public void Configure(EntityTypeBuilder<SecretCredential> builder)
    {
        builder.ToTable("SecretCredentials");
        builder.ConfigureBaseAuditableEntity();

        builder.Property(e => e.Scope).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        // Valor cifrado en reposo; nunca en claro.
        builder.Property(e => e.EncryptedValue).HasColumnType("text").IsRequired();

        builder.HasIndex(e => new { e.Scope, e.ClientId, e.Type }).IsUnique();
    }
}
