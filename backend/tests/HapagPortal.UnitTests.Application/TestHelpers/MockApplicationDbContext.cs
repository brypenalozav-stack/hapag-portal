namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class MockApplicationDbContext : IApplicationDbContext
{
    public List<Client> ClientList { get; } = [];
    public List<User> UserList { get; } = [];
    public List<UserRole> UserRoleList { get; } = [];
    public List<BillOfLading> BillsOfLadingList { get; } = [];
    public List<BLContainer> BLContainerList { get; } = [];
    public List<LocalCharge> LocalChargeList { get; } = [];
    public List<DemurrageCharge> DemurrageChargeList { get; } = [];
    public List<Payment> PaymentList { get; } = [];
    public List<PaymentDetail> PaymentDetailList { get; } = [];
    public List<CreditClient> CreditClientList { get; } = [];
    public List<DemurrageExemption> DemurrageExemptionList { get; } = [];
    public List<WarehouseChange> WarehouseChangeList { get; } = [];
    public List<ServiceOrder> ServiceOrderList { get; } = [];
    public List<FAQ> FAQList { get; } = [];
    public List<AuditLog> AuditLogList { get; } = [];
    public List<TaxConfiguration> TaxConfigurationList { get; } = [];
    public List<Currency> CurrencyList { get; } = [];

    public DbSet<Client> Clients => MockDbSetHelper.CreateMockDbSet(ClientList);
    public DbSet<User> Users => MockDbSetHelper.CreateMockDbSet(UserList);
    public DbSet<UserRole> UserRoles => MockDbSetHelper.CreateMockDbSet(UserRoleList);
    public DbSet<BillOfLading> BillsOfLading => MockDbSetHelper.CreateMockDbSet(BillsOfLadingList);
    public DbSet<BLContainer> BLContainers => MockDbSetHelper.CreateMockDbSet(BLContainerList);
    public DbSet<LocalCharge> LocalCharges => MockDbSetHelper.CreateMockDbSet(LocalChargeList);
    public DbSet<DemurrageCharge> DemurrageCharges => MockDbSetHelper.CreateMockDbSet(DemurrageChargeList);
    public DbSet<Payment> Payments => MockDbSetHelper.CreateMockDbSet(PaymentList);
    public DbSet<PaymentDetail> PaymentDetails => MockDbSetHelper.CreateMockDbSet(PaymentDetailList);
    public DbSet<CreditClient> CreditClients => MockDbSetHelper.CreateMockDbSet(CreditClientList);
    public DbSet<DemurrageExemption> DemurrageExemptions => MockDbSetHelper.CreateMockDbSet(DemurrageExemptionList);
    public DbSet<WarehouseChange> WarehouseChanges => MockDbSetHelper.CreateMockDbSet(WarehouseChangeList);
    public DbSet<ServiceOrder> ServiceOrders => MockDbSetHelper.CreateMockDbSet(ServiceOrderList);
    public DbSet<FAQ> FAQs => MockDbSetHelper.CreateMockDbSet(FAQList);
    public DbSet<AuditLog> AuditLogs => MockDbSetHelper.CreateMockDbSet(AuditLogList);
    public DbSet<TaxConfiguration> TaxConfigurations => MockDbSetHelper.CreateMockDbSet(TaxConfigurationList);
    public DbSet<Currency> Currencies => MockDbSetHelper.CreateMockDbSet(CurrencyList);

    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }
}
