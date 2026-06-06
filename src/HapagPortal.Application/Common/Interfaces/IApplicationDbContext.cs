namespace HapagPortal.Application.Common.Interfaces;

using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IApplicationDbContext
{
    DbSet<Client> Clients { get; }
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<BillOfLading> BillsOfLading { get; }
    DbSet<BLContainer> BLContainers { get; }
    DbSet<LocalCharge> LocalCharges { get; }
    DbSet<DemurrageCharge> DemurrageCharges { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentDetail> PaymentDetails { get; }
    DbSet<CreditClient> CreditClients { get; }
    DbSet<DemurrageExemption> DemurrageExemptions { get; }
    DbSet<WarehouseChange> WarehouseChanges { get; }
    DbSet<ServiceOrder> ServiceOrders { get; }
    DbSet<FAQ> FAQs { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<TaxConfiguration> TaxConfigurations { get; }
    DbSet<Currency> Currencies { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
