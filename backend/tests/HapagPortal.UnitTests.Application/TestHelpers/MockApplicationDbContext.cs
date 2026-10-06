namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class MockApplicationDbContext : IApplicationDbContext
{
    public List<Client> ClientList { get; } = [];
    public List<User> UserList { get; } = [];
    public List<UserRole> UserRoleList { get; } = [];
    public List<Role> RoleList { get; } = [];
    public List<Permission> PermissionList { get; } = [];
    public List<RolePermission> RolePermissionList { get; } = [];
    public List<ConfigurationSetting> ConfigurationSettingList { get; } = [];
    public List<SecretCredential> SecretCredentialList { get; } = [];
    public List<BillOfLading> BillsOfLadingList { get; } = [];
    public List<BLContainer> BLContainerList { get; } = [];
    public List<BLParty> BLPartyList { get; } = [];
    public List<BLCargoItem> BLCargoItemList { get; } = [];
    public List<ShipmentRole> ShipmentRoleList { get; } = [];
    public List<ShipmentAction> ShipmentActionList { get; } = [];
    public List<ShipmentAccessRule> ShipmentAccessRuleList { get; } = [];
    public List<OrganizationDocument> OrganizationDocumentList { get; } = [];
    public List<AccessGrant> AccessGrantList { get; } = [];
    public List<DefaultGrantee> DefaultGranteeList { get; } = [];
    public List<OpenAccessSetting> OpenAccessSettingList { get; } = [];
    public List<ShipmentAssociation> ShipmentAssociationList { get; } = [];
    public List<VisibilityWidening> VisibilityWideningList { get; } = [];
    public List<AccessAuditEntry> AccessAuditEntryList { get; } = [];
    public List<CustomsManifest> CustomsManifestList { get; } = [];
    public List<CustomsTransmission> CustomsTransmissionList { get; } = [];
    public List<CustomsTransmissionEvent> CustomsTransmissionEventList { get; } = [];
    public List<DeadlineRule> DeadlineRuleList { get; } = [];
    public List<DeadlineInstance> DeadlineInstanceList { get; } = [];
    public List<Notification> NotificationList { get; } = [];
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
    public List<ChargeConcept> ChargeConceptList { get; } = [];
    public List<Tariff> TariffList { get; } = [];
    public List<TariffTier> TariffTierList { get; } = [];
    public List<MaintainerChangeLog> MaintainerChangeLogList { get; } = [];
    public List<InternalChargeRule> InternalChargeRuleList { get; } = [];
    public List<AppliedExemption> AppliedExemptionList { get; } = [];
    public List<ExchangeRateRecord> ExchangeRateRecordList { get; } = [];
    public List<WarehouseChangeBatch> WarehouseChangeBatchList { get; } = [];
    public List<WarehouseChangeBatchItem> WarehouseChangeBatchItemList { get; } = [];
    public List<BusinessHoliday> BusinessHolidayList { get; } = [];
    public List<Cart> CartList { get; } = [];
    public List<CartItem> CartItemList { get; } = [];
    public List<PaymentStatusChange> PaymentStatusChangeList { get; } = [];
    public List<PaymentOutboxMessage> PaymentOutboxMessageList { get; } = [];
    public List<PaymentCurrencyRule> PaymentCurrencyRuleList { get; } = [];
    public List<PaymentMethodConfig> PaymentMethodConfigList { get; } = [];
    public List<PaymentBlockWindow> PaymentBlockWindowList { get; } = [];
    public List<CustomerInvoice> CustomerInvoiceList { get; } = [];
    public List<ShipmentDocument> ShipmentDocumentList { get; } = [];
    public List<ShipmentDocumentEvent> ShipmentDocumentEventList { get; } = [];
    public List<ShipmentPublicationRule> ShipmentPublicationRuleList { get; } = [];
    public List<KnowledgeArticle> KnowledgeArticleList { get; } = [];
    public List<AssistantMailbox> AssistantMailboxList { get; } = [];
    public List<AssistantSession> AssistantSessionList { get; } = [];
    public List<AssistantMessage> AssistantMessageList { get; } = [];
    public List<DangerousGood> DangerousGoodList { get; } = [];
    public List<TatcBatch> TatcBatchList { get; } = [];
    public List<TatcBatchItem> TatcBatchItemList { get; } = [];
    public List<ServiceDefinition> ServiceDefinitionList { get; } = [];
    public List<ServiceRequest> ServiceRequestList { get; } = [];
    public List<ServiceRequestEvent> ServiceRequestEventList { get; } = [];
    public List<ServiceRequestAttachment> ServiceRequestAttachmentList { get; } = [];
    public List<ServiceRequestCharge> ServiceRequestChargeList { get; } = [];

    public DbSet<Client> Clients => MockDbSetHelper.CreateMockDbSet(ClientList);
    public DbSet<User> Users => MockDbSetHelper.CreateMockDbSet(UserList);
    public DbSet<UserRole> UserRoles => MockDbSetHelper.CreateMockDbSet(UserRoleList);
    public DbSet<Role> Roles => MockDbSetHelper.CreateMockDbSet(RoleList);
    public DbSet<Permission> Permissions => MockDbSetHelper.CreateMockDbSet(PermissionList);
    public DbSet<RolePermission> RolePermissions => MockDbSetHelper.CreateMockDbSet(RolePermissionList);
    public DbSet<ConfigurationSetting> ConfigurationSettings => MockDbSetHelper.CreateMockDbSet(ConfigurationSettingList);
    public DbSet<SecretCredential> SecretCredentials => MockDbSetHelper.CreateMockDbSet(SecretCredentialList);
    public DbSet<BillOfLading> BillsOfLading => MockDbSetHelper.CreateMockDbSet(BillsOfLadingList);
    public DbSet<BLContainer> BLContainers => MockDbSetHelper.CreateMockDbSet(BLContainerList);
    public DbSet<BLParty> BLParties => MockDbSetHelper.CreateMockDbSet(BLPartyList);
    public DbSet<BLCargoItem> BLCargoItems => MockDbSetHelper.CreateMockDbSet(BLCargoItemList);
    public DbSet<ShipmentRole> ShipmentRoles => MockDbSetHelper.CreateMockDbSet(ShipmentRoleList);
    public DbSet<ShipmentAction> ShipmentActions => MockDbSetHelper.CreateMockDbSet(ShipmentActionList);
    public DbSet<ShipmentAccessRule> ShipmentAccessRules => MockDbSetHelper.CreateMockDbSet(ShipmentAccessRuleList);
    public DbSet<OrganizationDocument> OrganizationDocuments => MockDbSetHelper.CreateMockDbSet(OrganizationDocumentList);
    public DbSet<AccessGrant> AccessGrants => MockDbSetHelper.CreateMockDbSet(AccessGrantList);
    public DbSet<DefaultGrantee> DefaultGrantees => MockDbSetHelper.CreateMockDbSet(DefaultGranteeList);
    public DbSet<OpenAccessSetting> OpenAccessSettings => MockDbSetHelper.CreateMockDbSet(OpenAccessSettingList);
    public DbSet<ShipmentAssociation> ShipmentAssociations => MockDbSetHelper.CreateMockDbSet(ShipmentAssociationList);
    public DbSet<VisibilityWidening> VisibilityWidenings => MockDbSetHelper.CreateMockDbSet(VisibilityWideningList);
    public DbSet<AccessAuditEntry> AccessAuditEntries => MockDbSetHelper.CreateMockDbSet(AccessAuditEntryList);
    public DbSet<CustomsManifest> CustomsManifests => MockDbSetHelper.CreateMockDbSet(CustomsManifestList);
    public DbSet<CustomsTransmission> CustomsTransmissions => MockDbSetHelper.CreateMockDbSet(CustomsTransmissionList);
    public DbSet<CustomsTransmissionEvent> CustomsTransmissionEvents => MockDbSetHelper.CreateMockDbSet(CustomsTransmissionEventList);
    public DbSet<DeadlineRule> DeadlineRules => MockDbSetHelper.CreateMockDbSet(DeadlineRuleList);
    public DbSet<DeadlineInstance> DeadlineInstances => MockDbSetHelper.CreateMockDbSet(DeadlineInstanceList);
    public DbSet<Notification> Notifications => MockDbSetHelper.CreateMockDbSet(NotificationList);
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
    public DbSet<ChargeConcept> ChargeConcepts => MockDbSetHelper.CreateMockDbSet(ChargeConceptList);
    public DbSet<Tariff> Tariffs => MockDbSetHelper.CreateMockDbSet(TariffList);
    public DbSet<TariffTier> TariffTiers => MockDbSetHelper.CreateMockDbSet(TariffTierList);
    public DbSet<MaintainerChangeLog> MaintainerChangeLogs => MockDbSetHelper.CreateMockDbSet(MaintainerChangeLogList);
    public DbSet<InternalChargeRule> InternalChargeRules => MockDbSetHelper.CreateMockDbSet(InternalChargeRuleList);
    public DbSet<AppliedExemption> AppliedExemptions => MockDbSetHelper.CreateMockDbSet(AppliedExemptionList);
    public DbSet<ExchangeRateRecord> ExchangeRateRecords => MockDbSetHelper.CreateMockDbSet(ExchangeRateRecordList);
    public DbSet<WarehouseChangeBatch> WarehouseChangeBatches => MockDbSetHelper.CreateMockDbSet(WarehouseChangeBatchList);
    public DbSet<WarehouseChangeBatchItem> WarehouseChangeBatchItems => MockDbSetHelper.CreateMockDbSet(WarehouseChangeBatchItemList);
    public DbSet<BusinessHoliday> BusinessHolidays => MockDbSetHelper.CreateMockDbSet(BusinessHolidayList);
    public DbSet<Cart> Carts => MockDbSetHelper.CreateMockDbSet(CartList);
    public DbSet<CartItem> CartItems => MockDbSetHelper.CreateMockDbSet(CartItemList);
    public DbSet<PaymentStatusChange> PaymentStatusChanges => MockDbSetHelper.CreateMockDbSet(PaymentStatusChangeList);
    public DbSet<PaymentOutboxMessage> PaymentOutboxMessages => MockDbSetHelper.CreateMockDbSet(PaymentOutboxMessageList);
    public DbSet<PaymentCurrencyRule> PaymentCurrencyRules => MockDbSetHelper.CreateMockDbSet(PaymentCurrencyRuleList);
    public DbSet<PaymentMethodConfig> PaymentMethodConfigs => MockDbSetHelper.CreateMockDbSet(PaymentMethodConfigList);
    public DbSet<PaymentBlockWindow> PaymentBlockWindows => MockDbSetHelper.CreateMockDbSet(PaymentBlockWindowList);
    public DbSet<CustomerInvoice> CustomerInvoices => MockDbSetHelper.CreateMockDbSet(CustomerInvoiceList);
    public DbSet<ShipmentDocument> ShipmentDocuments => MockDbSetHelper.CreateMockDbSet(ShipmentDocumentList);
    public DbSet<ShipmentDocumentEvent> ShipmentDocumentEvents => MockDbSetHelper.CreateMockDbSet(ShipmentDocumentEventList);
    public DbSet<ShipmentPublicationRule> ShipmentPublicationRules => MockDbSetHelper.CreateMockDbSet(ShipmentPublicationRuleList);
    public DbSet<KnowledgeArticle> KnowledgeArticles => MockDbSetHelper.CreateMockDbSet(KnowledgeArticleList);
    public DbSet<AssistantMailbox> AssistantMailboxes => MockDbSetHelper.CreateMockDbSet(AssistantMailboxList);
    public DbSet<AssistantSession> AssistantSessions => MockDbSetHelper.CreateMockDbSet(AssistantSessionList);
    public DbSet<AssistantMessage> AssistantMessages => MockDbSetHelper.CreateMockDbSet(AssistantMessageList);
    public DbSet<DangerousGood> DangerousGoods => MockDbSetHelper.CreateMockDbSet(DangerousGoodList);
    public DbSet<TatcBatch> TatcBatches => MockDbSetHelper.CreateMockDbSet(TatcBatchList);
    public DbSet<TatcBatchItem> TatcBatchItems => MockDbSetHelper.CreateMockDbSet(TatcBatchItemList);
    public DbSet<ServiceDefinition> ServiceDefinitions => MockDbSetHelper.CreateMockDbSet(ServiceDefinitionList);
    public DbSet<ServiceRequest> ServiceRequests => MockDbSetHelper.CreateMockDbSet(ServiceRequestList);
    public DbSet<ServiceRequestEvent> ServiceRequestEvents => MockDbSetHelper.CreateMockDbSet(ServiceRequestEventList);
    public DbSet<ServiceRequestAttachment> ServiceRequestAttachments => MockDbSetHelper.CreateMockDbSet(ServiceRequestAttachmentList);
    public DbSet<ServiceRequestCharge> ServiceRequestCharges => MockDbSetHelper.CreateMockDbSet(ServiceRequestChargeList);

    public int SaveChangesCallCount { get; private set; }

    /// <summary>
    /// Excepción que lanzará el próximo guardado (p. ej. <c>DbUpdateConcurrencyException</c> para simular
    /// que otra solicitud cambió las mismas filas); se consume al lanzarse.
    /// </summary>
    public Exception? NextSaveChangesException { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        if (NextSaveChangesException is { } exception)
        {
            NextSaveChangesException = null;
            throw exception;
        }

        return Task.FromResult(1);
    }
}
