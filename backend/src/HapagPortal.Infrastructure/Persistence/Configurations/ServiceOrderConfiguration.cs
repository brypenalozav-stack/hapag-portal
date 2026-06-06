using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HapagPortal.Infrastructure.Persistence.Configurations;

internal sealed class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ConfigureBaseAuditableEntity();

        builder.ToTable("ServiceOrders");

        builder.Property(e => e.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.OrderType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Country)
            .HasMaxLength(5)
            .IsRequired();

        builder.HasIndex(e => e.OrderNumber)
            .IsUnique();

        builder.HasOne(e => e.BillOfLading)
            .WithMany()
            .HasForeignKey(e => e.BillOfLadingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
