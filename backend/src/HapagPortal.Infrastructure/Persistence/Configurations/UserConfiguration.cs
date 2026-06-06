using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("Users");

        builder.Property(e => e.Username)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.UserType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.HasIndex(e => e.Username)
            .IsUnique();

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Roles)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
