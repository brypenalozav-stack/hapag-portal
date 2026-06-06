using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RoleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.RoleName })
            .IsUnique();
    }
}
