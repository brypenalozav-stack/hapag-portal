using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HapagPortal.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditInfo(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void ApplyAuditInfo(DbContext? context)
    {
        if (context is null) return;

        // M1-23: la auditoría de accesos es append-only.
        if (context.ChangeTracker.Entries<AccessAuditEntry>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Access audit entries are append-only and cannot be modified or deleted.");

        // NF-15: el registro de cambios de los mantenedores también es append-only.
        if (context.ChangeTracker.Entries<MaintainerChangeLog>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Maintainer change log entries are append-only and cannot be modified or deleted.");

        // NF-02: el historial de estados de los pagos también es append-only.
        if (context.ChangeTracker.Entries<PaymentStatusChange>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Payment status history entries are append-only and cannot be modified or deleted.");

        // M1-06: el registro de cambios de las listas de distribución también es append-only.
        if (context.ChangeTracker.Entries<ContactListChange>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Contact list change entries are append-only and cannot be modified or deleted.");

        var now = DateTime.UtcNow;

        // M8-08: durante una «Vista como cliente» los cambios quedan con la identidad del usuario interno que la inició.
        var userId = (currentUserService.ImpersonatorUserId ?? currentUserService.UserId)?.ToString() ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = userId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = userId;
                    break;
            }
        }
    }
}
