using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.PostPayment;
using HapagPortal.Application.InternalChargeRules;
using HapagPortal.Application.Payments.Maintainers;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Domain.Access;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HapagPortal.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ConfigurationSetting> ConfigurationSettings => Set<ConfigurationSetting>();
    public DbSet<SecretCredential> SecretCredentials => Set<SecretCredential>();
    public DbSet<BillOfLading> BillsOfLading => Set<BillOfLading>();
    public DbSet<BLContainer> BLContainers => Set<BLContainer>();
    public DbSet<BLParty> BLParties => Set<BLParty>();
    public DbSet<BLCargoItem> BLCargoItems => Set<BLCargoItem>();
    public DbSet<ShipmentRole> ShipmentRoles => Set<ShipmentRole>();
    public DbSet<ShipmentAction> ShipmentActions => Set<ShipmentAction>();
    public DbSet<ShipmentAccessRule> ShipmentAccessRules => Set<ShipmentAccessRule>();
    public DbSet<OrganizationDocument> OrganizationDocuments => Set<OrganizationDocument>();
    public DbSet<AccessGrant> AccessGrants => Set<AccessGrant>();
    public DbSet<DefaultGrantee> DefaultGrantees => Set<DefaultGrantee>();
    public DbSet<OpenAccessSetting> OpenAccessSettings => Set<OpenAccessSetting>();
    public DbSet<ShipmentAssociation> ShipmentAssociations => Set<ShipmentAssociation>();
    public DbSet<VisibilityWidening> VisibilityWidenings => Set<VisibilityWidening>();
    public DbSet<AccessAuditEntry> AccessAuditEntries => Set<AccessAuditEntry>();
    public DbSet<CustomsManifest> CustomsManifests => Set<CustomsManifest>();
    public DbSet<CustomsTransmission> CustomsTransmissions => Set<CustomsTransmission>();
    public DbSet<CustomsTransmissionEvent> CustomsTransmissionEvents => Set<CustomsTransmissionEvent>();
    public DbSet<DeadlineRule> DeadlineRules => Set<DeadlineRule>();
    public DbSet<DeadlineInstance> DeadlineInstances => Set<DeadlineInstance>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<LocalCharge> LocalCharges => Set<LocalCharge>();
    public DbSet<DemurrageCharge> DemurrageCharges => Set<DemurrageCharge>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentDetail> PaymentDetails => Set<PaymentDetail>();
    public DbSet<CreditClient> CreditClients => Set<CreditClient>();
    public DbSet<DemurrageExemption> DemurrageExemptions => Set<DemurrageExemption>();
    public DbSet<WarehouseChange> WarehouseChanges => Set<WarehouseChange>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<FAQ> FAQs => Set<FAQ>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TaxConfiguration> TaxConfigurations => Set<TaxConfiguration>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ChargeConcept> ChargeConcepts => Set<ChargeConcept>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<TariffTier> TariffTiers => Set<TariffTier>();
    public DbSet<MaintainerChangeLog> MaintainerChangeLogs => Set<MaintainerChangeLog>();
    public DbSet<InternalChargeRule> InternalChargeRules => Set<InternalChargeRule>();
    public DbSet<AppliedExemption> AppliedExemptions => Set<AppliedExemption>();
    public DbSet<ExchangeRateRecord> ExchangeRateRecords => Set<ExchangeRateRecord>();
    public DbSet<WarehouseChangeBatch> WarehouseChangeBatches => Set<WarehouseChangeBatch>();
    public DbSet<WarehouseChangeBatchItem> WarehouseChangeBatchItems => Set<WarehouseChangeBatchItem>();
    public DbSet<BusinessHoliday> BusinessHolidays => Set<BusinessHoliday>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<PaymentStatusChange> PaymentStatusChanges => Set<PaymentStatusChange>();
    public DbSet<PaymentOutboxMessage> PaymentOutboxMessages => Set<PaymentOutboxMessage>();
    public DbSet<PaymentCurrencyRule> PaymentCurrencyRules => Set<PaymentCurrencyRule>();
    public DbSet<PaymentMethodConfig> PaymentMethodConfigs => Set<PaymentMethodConfig>();
    public DbSet<PaymentBlockWindow> PaymentBlockWindows => Set<PaymentBlockWindow>();
    public DbSet<CustomerInvoice> CustomerInvoices => Set<CustomerInvoice>();
    public DbSet<ShipmentDocument> ShipmentDocuments => Set<ShipmentDocument>();
    public DbSet<ShipmentDocumentEvent> ShipmentDocumentEvents => Set<ShipmentDocumentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        SeedCurrencies(modelBuilder);
        SeedTaxConfigurations(modelBuilder);
        SeedAdminClientAndUser(modelBuilder);
        SeedFAQs(modelBuilder);
        SeedDemoData(modelBuilder);
        SeedRbac(modelBuilder);
        SeedDeadlineRules(modelBuilder);
        SeedAccessMatrix(modelBuilder);
        SeedOrganizationAccessDemo(modelBuilder);
        SeedThirdPartyAccessDemo(modelBuilder);
        SeedChargeCatalogAndTariffs(modelBuilder);
        SeedChargeRulesDemo(modelBuilder);
        SeedPaymentConfiguration(modelBuilder);
        SeedPaymentsDemo(modelBuilder);
        SeedDocumentsDemo(modelBuilder);
    }

    /// <summary>
    /// Fase 1 Ola E: documentos del embarque de demostración (M6-09) y los escenarios que los muestran. BL12
    /// es Collect con la agencia de aduanas como parte (M6-04: solo ella ve el comprobante); BL13 es de
    /// Global Forwarding (FFWW, M4-04) con carta de responsabilidad vigente, mientras BL11 sigue sin carta y
    /// bloqueado. BL05 tiene las demoras anticipadas pagadas (M3-16), de modo que su CLD (M6-07) se puede
    /// emitir y el de BL04 muestra los bloqueos. Los documentos sembrados no tienen archivo: se generan desde
    /// su modelo en la primera descarga (se firman en ese momento los que lo exigen).
    /// </summary>
    private static void SeedDocumentsDemo(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var settings = new DocumentSettings();

        var bl12 = new BillOfLading
        {
            Id = SeedDataIds.BL12,
            BLNumber = "HLCUSAI260501240",
            BookingNumber = "HLCUBKG2605124",
            ShipmentType = "Import",
            Vessel = "Cartagena Express",
            Voyage = "2611E",
            PortOfLoading = "Yokohama (JPYOK)",
            PortOfDischarge = "San Antonio (CLSAI)",
            PlaceOfDelivery = "Santiago, Chile",
            ETD = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Importadora Demo SpA",
            Shipper = "Yokohama Machinery Co.",
            NotifyParty = "Agencia Marítima del Pacífico Ltda",
            FreightAmount = 4800m,
            FreightCurrency = "USD",
            FreightTerms = PaymentDocumentRules.CollectFreightTerms,
            FreightPaidAt = new DateTime(2026, 10, 3, 15, 0, 0, DateTimeKind.Utc),
            Status = "Arrived",
            Country = CountryCodes.Chile,
            ClientId = SeedDataIds.DemoClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        };

        var bl13 = new BillOfLading
        {
            Id = SeedDataIds.BL13,
            BLNumber = "HLCUVAP260501350",
            BookingNumber = "HLCUBKG2605135",
            ShipmentType = "Import",
            Vessel = "Callao Express",
            Voyage = "2611N",
            PortOfLoading = "Shanghai (CNSHA)",
            PortOfDischarge = "Valparaiso (CLVAP)",
            PlaceOfDelivery = "Valparaiso, Chile",
            ETD = new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Global Forwarding Chile SpA",
            Shipper = "Shanghai Furniture Ltd",
            FreightAmount = 2900m,
            FreightCurrency = "USD",
            Status = "Arrived",
            Country = CountryCodes.Chile,
            ClientId = SeedDataIds.FfwwDemoClient,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        };

        modelBuilder.Entity<BillOfLading>().HasData(bl12, bl13);

        var container15 = new BLContainer { Id = SeedDataIds.Container15, ContainerNumber = "HLXU3045001", ContainerType = "40HC", SealNumber = "SL-045001", Weight = 25100m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL12, CreatedAt = now, CreatedBy = "SYSTEM" };
        var container16 = new BLContainer { Id = SeedDataIds.Container16, ContainerNumber = "HLXU3045002", ContainerType = "20DV", SealNumber = "SL-045002", Weight = 17900m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL13, CreatedAt = now, CreatedBy = "SYSTEM" };
        modelBuilder.Entity<BLContainer>().HasData(container15, container16);

        modelBuilder.Entity<LocalCharge>().HasData(
            // Cargo del FFWW sobre BL13: con la carta vigente puede agregarse al carro (M4-04 cumplido).
            new LocalCharge { Id = SeedDataIds.LocalCharge23, ChargeType = ChargeConceptCodes.Thc, Description = "Terminal Handling Charge - 20DV (Valparaíso)", Amount = 185000m, Currency = "CLP", Status = ChargeStatus.Pending, IsTaxable = true, TaxRate = 19m, TaxAmount = 35150m, TotalAmount = 220150m, BillOfLadingId = SeedDataIds.BL13, CreatedAt = now, CreatedBy = "SYSTEM" },
            // Servicio de certificado de transbordo pagado de BL06 (origen del certificado sembrado).
            new LocalCharge { Id = SeedDataIds.LocalCharge24, ChargeType = ChargeConceptCodes.TransshipmentCertificate, Description = "Certificado de transbordo", Amount = 35000m, Currency = "CLP", Status = ChargeStatus.Paid, IsTaxable = true, TaxRate = 19m, TaxAmount = 6650m, TotalAmount = 41650m, BillOfLadingId = SeedDataIds.BL06, CreatedAt = now, CreatedBy = "SYSTEM" },
            // Demoras anticipadas pagadas de BL05 (M3-16): el CLD de BL05 no queda bloqueado.
            new LocalCharge { Id = SeedDataIds.LocalCharge25, ChargeType = ChargeConceptCodes.AdvanceDemurrageBo, Description = "Demoras anticipadas (1 contenedor(es))", Amount = 150m, Currency = "USD", Status = ChargeStatus.Paid, IsTaxable = false, TaxRate = 0m, TaxAmount = 0m, TotalAmount = 150m, BillOfLadingId = SeedDataIds.BL05, CreatedAt = now, CreatedBy = "SYSTEM" });

        var shipmentRoles = new (Guid BlId, Guid ClientId, string Role)[]
        {
            (SeedDataIds.BL12, SeedDataIds.DemoClientCL, ShipmentRoleCodes.Consignee),
            (SeedDataIds.BL12, SeedDataIds.AgentClientCL, ShipmentRoleCodes.CustomsAgency),
            (SeedDataIds.BL13, SeedDataIds.FfwwDemoClient, ShipmentRoleCodes.Consignee),
        };

        modelBuilder.Entity<ShipmentRole>().HasData(shipmentRoles.Select(r => new ShipmentRole
        {
            Id = DeterministicGuid($"shipment-role:{r.BlId}:{r.ClientId}:{r.Role}"),
            BillOfLadingId = r.BlId,
            ClientId = r.ClientId,
            Role = r.Role,
            Source = ShipmentRoleSources.Seed,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        }));

        // Datos de los BL ya sembrados que usan las plantillas (copia de sus valores).
        var bl01 = new BillOfLading
        {
            Id = SeedDataIds.BL01, BLNumber = "HLCUVAL250100123", BookingNumber = "HLCUBKG2501001", ShipmentType = "Import",
            Vessel = "Hamburg Express", Voyage = "025E", PortOfLoading = "Shanghai (CNSHA)", PortOfDischarge = "San Antonio (CLSAI)",
            PlaceOfDelivery = "Santiago, Chile", ETD = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), Consignee = "Importadora Demo SpA",
            Shipper = "Shanghai Electronics Co. Ltd", NotifyParty = "Agencia Marítima del Pacífico Ltda", FreightAmount = 3500m,
            FreightCurrency = "USD", Status = "Arrived", Country = CountryCodes.Chile, ClientId = SeedDataIds.DemoClientCL
        };
        var bl06 = new BillOfLading
        {
            Id = SeedDataIds.BL06, BLNumber = "HLCUSAI260300610", BookingNumber = "HLCUBKG2603061", ShipmentType = "Export",
            Vessel = "Valparaiso Express", Voyage = "2610S", PortOfLoading = "San Antonio (CLSAI)", PortOfDischarge = "Rotterdam (NLRTM)",
            PlaceOfDelivery = "Rotterdam, Netherlands", ETD = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc), Shipper = "Importadora Demo SpA", Consignee = "Fruit Import BV",
            FreightAmount = 3900m, FreightCurrency = "USD", Status = "Booked", Country = CountryCodes.Chile, ClientId = SeedDataIds.DemoClientCL
        };
        var bl01Containers = new List<BLContainer>
        {
            new() { ContainerNumber = "HLXU1234567", ContainerType = "40HC", SealNumber = "SL-001234", Weight = 24500m, Status = "Discharged" },
            new() { ContainerNumber = "HLXU7654321", ContainerType = "20DV", SealNumber = "SL-005678", Weight = 18200m, Status = "Discharged" }
        };
        var bl06Containers = new List<BLContainer>
        {
            new() { ContainerNumber = "HLXU2023001", ContainerType = "40RF", SealNumber = "SL-020301", Weight = 26800m, Status = "GateIn" }
        };
        var bl12Containers = new List<BLContainer> { container15 };
        var bl13Containers = new List<BLContainer> { container16 };

        ShipmentDocumentData Data(BillOfLading bl, List<BLContainer> containers) => new(bl, [], containers, [], []);

        DocumentHeader Header(string number, BillOfLading bl, DateTime issuedAt) =>
            new(number, issuedAt, ShipmentDocumentTemplates.VerificationCode(number, bl.BLNumber, issuedAt), settings.IssuerFor(bl.Country));

        ShipmentDocument Document(
            Guid id, string type, string number, BillOfLading bl, List<BLContainer> containers, DateTime issuedAt,
            Guid organizationId, string? email, PdfDocumentModel model, string? recipients = null) => new()
        {
            Id = id,
            DocumentType = type,
            DocumentNumber = number,
            Status = ShipmentDocumentStatus.Issued,
            BillOfLadingId = bl.Id,
            BlNumber = bl.BLNumber,
            BookingNumber = bl.BookingNumber,
            Country = bl.Country,
            ContainerNumbers = string.Join(',', containers.Select(c => c.ContainerNumber)),
            IssuedAt = issuedAt,
            IssuedForOrganizationId = organizationId,
            IssuedByEmail = email,
            Origin = ShipmentDocumentOrigins.Seed,
            FileName = ShipmentDocumentTemplates.FileName(type, number),
            ContentType = ShipmentDocumentService.PdfContentType,
            VerificationCode = model.VerificationCode!,
            TemplateJson = JsonSerializer.Serialize(model, ShipmentDocumentService.JsonOptions),
            RecipientEmails = recipients,
            DeliveredAt = recipients is null ? null : issuedAt,
            RetainUntil = issuedAt.AddYears(settings.RetentionYears),
            CreatedAt = issuedAt,
            CreatedBy = email ?? "SYSTEM"
        };

        var transshipmentAt = new DateTime(2026, 10, 4, 13, 0, 0, DateTimeKind.Utc);
        var copyAt = new DateTime(2026, 10, 3, 16, 30, 0, DateTimeKind.Utc);
        var collectAt = new DateTime(2026, 10, 3, 15, 5, 0, DateTimeKind.Utc);
        var letterAt = new DateTime(2026, 10, 2, 14, 0, 0, DateTimeKind.Utc);

        const string transshipmentNumber = "CTB-20261004-5A1B2C3D";
        const string copyNumber = "CBL-20261003-7E8F9A0B";
        const string collectNumber = "CCO-20261003-1C2D3E4F";
        const string letterNumber = "CRE-20261002-9F8E7D6C";

        var letter = Document(
            SeedDataIds.DocumentLetterBL13, ShipmentDocumentTypes.ResponsibilityLetter, letterNumber, bl13,
            bl13Containers, letterAt, SeedDataIds.FfwwDemoClient, "ffww@globalforwarding.cl",
            ShipmentDocumentTemplates.ResponsibilityLetter(
                Data(bl13, bl13Containers), Header(letterNumber, bl13, letterAt),
                new ResponsibilityLetterData(
                    "Global Forwarding Chile SpA", "76000003-3", "Felipe Forwarder", "12.345.678-5", "Gerente de Operaciones",
                    "ffww@globalforwarding.cl", null, "Muebles de madera", null, ResponsibilityLetterTerms.Version, letterAt)));
        letter.TermsVersion = ResponsibilityLetterTerms.Version;
        letter.TermsAcceptedAt = letterAt;

        var documents = new[]
        {
            Document(
                SeedDataIds.DocumentTransshipmentBL06, ShipmentDocumentTypes.TransshipmentCertificate, transshipmentNumber, bl06,
                bl06Containers, transshipmentAt, SeedDataIds.DemoClientCL, null,
                ShipmentDocumentTemplates.TransshipmentCertificate(
                    Data(bl06, bl06Containers), Header(transshipmentNumber, bl06, transshipmentAt), "Importadora Demo SpA", "76123456-7", null),
                recipients: "demo@importadorademo.cl"),
            Document(
                SeedDataIds.DocumentBlCopyBL01, ShipmentDocumentTypes.BlCopyNonValued, copyNumber, bl01,
                bl01Containers, copyAt, SeedDataIds.DemoClientCL, "demo@importadorademo.cl",
                ShipmentDocumentTemplates.BlCopy(
                    Data(bl01, bl01Containers), Header(copyNumber, bl01, copyAt), valued: false, "Importadora Demo SpA (demo@importadorademo.cl)"),
                recipients: "demo@importadorademo.cl"),
            Document(
                SeedDataIds.DocumentCollectBL12, ShipmentDocumentTypes.CollectReceipt, collectNumber, bl12,
                bl12Containers, collectAt, SeedDataIds.AgentClientCL, null,
                ShipmentDocumentTemplates.CollectReceipt(
                    Data(bl12, bl12Containers), Header(collectNumber, bl12, collectAt), "Agencia Marítima del Pacífico Ltda", "96555444-3",
                    null, null, 4800m, "USD", bl12.FreightPaidAt)),
            letter,
        };

        modelBuilder.Entity<ShipmentDocument>().HasData(documents);

        modelBuilder.Entity<ShipmentDocumentEvent>().HasData(documents.Select(d => new ShipmentDocumentEvent
        {
            Id = DeterministicGuid($"document-event:{d.Id}:issued"),
            ShipmentDocumentId = d.Id,
            EventType = ShipmentDocumentEventTypes.Issued,
            Channel = d.IssuedByEmail is null ? DocumentChannels.System : DocumentChannels.Portal,
            OccurredAt = d.IssuedAt,
            UserEmail = d.IssuedByEmail,
            OrganizationId = d.IssuedForOrganizationId
        }));
    }

    /// <summary>
    /// Fase 1 Ola D: monedas por recargo del ejemplo de M5-04 (Fletes USD/EUR/CLP, Gate In y EDS CLP,
    /// Demurrage USD/CLP en Chile) y su par en Bolivia, medios de pago por país (M5-03: Khipu, botón de
    /// bancos, depósito con boleta y el espacio reservado de dólares digitales) y ventanas de bloqueo de
    /// pagos (M8-07), con su alta en el registro de cambios (NF-15).
    /// </summary>
    private static void SeedPaymentConfiguration(ModelBuilder modelBuilder)
    {
        var created = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        var currencies = new (string Country, string Concept, string[] Currencies)[]
        {
            (CountryCodes.Chile, PaymentConcepts.Freight, ["USD", "EUR", "CLP"]),
            (CountryCodes.Chile, ChargeConceptCodes.GateIn, ["CLP"]),
            (CountryCodes.Chile, ChargeConceptCodes.Eds, ["CLP"]),
            (CountryCodes.Chile, ChargeConceptCodes.Demurrage, ["USD", "CLP"]),
            (CountryCodes.Bolivia, PaymentConcepts.Freight, ["USD", "BOB"]),
            (CountryCodes.Bolivia, ChargeConceptCodes.GateIn, ["BOB"]),
            (CountryCodes.Bolivia, ChargeConceptCodes.Eds, ["BOB"]),
            (CountryCodes.Bolivia, ChargeConceptCodes.Demurrage, ["BOB", "USD"]),
            (CountryCodes.Bolivia, ChargeConceptCodes.AdvanceDemurrageBo, ["USD", "BOB"]),
        };

        var rules = currencies
            .SelectMany(c => c.Currencies.Select(currency => new PaymentCurrencyRule
            {
                Id = DeterministicGuid($"payment-currency:{c.Country}:{c.Concept}:{currency}"),
                Country = c.Country,
                ConceptCode = c.Concept,
                Currency = currency,
                IsEnabled = true,
                CreatedAt = created,
                CreatedBy = "SYSTEM"
            }))
            .ToList();

        modelBuilder.Entity<PaymentCurrencyRule>().HasData(rules);
        modelBuilder.Entity<MaintainerChangeLog>().HasData(rules.Select(r => new MaintainerChangeLog
        {
            Id = DeterministicGuid($"maintainer-log:payment-currency:{r.Id}"),
            Maintainer = MaintainerNames.PaymentCurrency,
            EntityId = r.Id,
            Action = MaintainerActions.Created,
            NewValue = JsonSerializer.Serialize(PaymentCurrencySnapshot.From(r), MaintainerChangeLogger.JsonOptions),
            ChangedAt = created,
            ChangedBy = "SYSTEM"
        }));

        var methods = new[]
        {
            Method(CountryCodes.Chile, PaymentMethodCodes.Khipu, "Khipu", "Transferencia simplificada con Khipu", PaymentMethodKinds.Online, PaymentProviderKeys.Khipu, "CLP", 10),
            Method(CountryCodes.Chile, PaymentMethodCodes.BankButtonBancoChile, "Botón de pago Banco de Chile", "Pago en línea desde la banca del Banco de Chile", PaymentMethodKinds.Online, PaymentProviderKeys.BancoChile, "CLP,USD", 20),
            Method(CountryCodes.Chile, PaymentMethodCodes.BankButtonSantander, "Botón de pago Santander", "Pago en línea desde la banca de Santander", PaymentMethodKinds.Online, PaymentProviderKeys.Santander, "CLP", 30),
            Method(CountryCodes.Chile, PaymentMethodCodes.BankButtonBci, "Botón de pago Bci", "Pago en línea desde la banca de Bci", PaymentMethodKinds.Online, PaymentProviderKeys.Bci, "CLP", 40),
            Method(CountryCodes.Chile, PaymentMethodCodes.Deposit, "Depósito bancario (boleta)", "Boleta para depósito o transferencia; Finanzas confirma el abono", PaymentMethodKinds.Deposit, null, "CLP,USD,EUR", 50),
            Method(CountryCodes.Chile, PaymentMethodCodes.DigitalUsd, "Dólares digitales", "Reservado (M5-03): se habilita cuando se defina el proveedor", PaymentMethodKinds.Online, null, "USD", 90, enabled: false),
            Method(CountryCodes.Bolivia, PaymentMethodCodes.Deposit, "Depósito o transferencia bancaria (boleta)", "Boleta para depósito o transferencia; Finanzas confirma el abono", PaymentMethodKinds.Deposit, null, "BOB,USD", 10),
            Method(CountryCodes.Bolivia, PaymentMethodCodes.DigitalUsd, "Dólares digitales", "Reservado (M5-03): se habilita cuando se defina el proveedor", PaymentMethodKinds.Online, null, "USD", 90, enabled: false),
        };

        foreach (var method in methods)
        {
            method.CreatedAt = created;
            method.CreatedBy = "SYSTEM";
        }

        modelBuilder.Entity<PaymentMethodConfig>().HasData(methods);
        modelBuilder.Entity<MaintainerChangeLog>().HasData(methods.Select(m => new MaintainerChangeLog
        {
            Id = DeterministicGuid($"maintainer-log:payment-method:{m.Id}"),
            Maintainer = MaintainerNames.PaymentMethod,
            EntityId = m.Id,
            Action = MaintainerActions.Created,
            NewValue = JsonSerializer.Serialize(PaymentMethodSnapshot.From(m), MaintainerChangeLogger.JsonOptions),
            ChangedAt = created,
            ChangedBy = "SYSTEM"
        }));

        var windows = new[]
        {
            new PaymentBlockWindow
            {
                Id = SeedDataIds.BlockWindowPast,
                Country = CountryCodes.Chile,
                StartDate = new DateOnly(2026, 9, 30),
                StartTime = new TimeOnly(20, 0),
                EndDate = new DateOnly(2026, 9, 30),
                EndTime = new TimeOnly(23, 59),
                Reason = "Cierre contable de septiembre",
                ClientMessage = "Los pagos están suspendidos temporalmente por el cierre contable mensual. Podrá pagar nuevamente desde las 23:59 (hora de Chile).",
                CreatedAt = created.AddDays(-5),
                CreatedBy = "SYSTEM"
            },
            new PaymentBlockWindow
            {
                Id = SeedDataIds.BlockWindowFuture,
                Country = null,
                StartDate = new DateOnly(2026, 10, 31),
                StartTime = new TimeOnly(21, 0),
                EndDate = new DateOnly(2026, 11, 1),
                EndTime = new TimeOnly(6, 0),
                Reason = "Cierre contable de octubre",
                ClientMessage = "Los pagos están suspendidos temporalmente por el cierre contable mensual. Podrá pagar nuevamente a partir de las 06:00 (hora local).",
                CreatedAt = created,
                CreatedBy = "SYSTEM"
            },
            new PaymentBlockWindow
            {
                Id = SeedDataIds.BlockWindowFutureBO,
                Country = CountryCodes.Bolivia,
                StartDate = new DateOnly(2026, 12, 24),
                StartTime = new TimeOnly(18, 0),
                EndDate = new DateOnly(2026, 12, 26),
                EndTime = new TimeOnly(8, 0),
                Reason = "Mantenimiento de la conciliación bancaria de fin de año",
                ClientMessage = "Los pagos en línea no están disponibles por mantenimiento hasta el 26-12 a las 08:00 (hora de Bolivia).",
                CreatedAt = created,
                CreatedBy = "SYSTEM"
            },
        };

        modelBuilder.Entity<PaymentBlockWindow>().HasData(windows);
        modelBuilder.Entity<MaintainerChangeLog>().HasData(windows.Select(w => new MaintainerChangeLog
        {
            Id = DeterministicGuid($"maintainer-log:payment-block:{w.Id}"),
            Maintainer = MaintainerNames.PaymentBlockWindow,
            EntityId = w.Id,
            Action = MaintainerActions.Created,
            NewValue = JsonSerializer.Serialize(PaymentBlockWindowSnapshot.From(w), MaintainerChangeLogger.JsonOptions),
            ChangedAt = w.CreatedAt,
            ChangedBy = "SYSTEM"
        }));

        static PaymentMethodConfig Method(
            string country, string code, string name, string description, string kind, string? provider, string currencies,
            int order, bool enabled = true) => new()
        {
            Id = DeterministicGuid($"payment-method:{country}:{code}"),
            Code = code,
            Name = name,
            Description = description,
            Country = country,
            Kind = kind,
            ProviderKey = provider,
            Currencies = currencies,
            IsEnabled = enabled,
            DisplayOrder = order
        };
    }

    /// <summary>
    /// Datos de demostración de la Ola D: facturas locales en la caché (M7-01) para Importadora Demo
    /// (factura de demurrage, vencida, pagada, sin folio, nota de crédito, en EUR), Comercial Altiplano y el
    /// cliente con crédito, y pagos históricos del portal (M7-02): uno del carro con Khipu, uno pagado por
    /// la agencia bajo mandato (pagador distinto del RUT de facturación), una boleta de depósito emitida
    /// (no anulable por el cliente, M5-02), uno fallido por indisponibilidad de la plataforma (NF-12) y
    /// uno de Bolivia confirmado por Finanzas, con su historial de estados (NF-02).
    /// </summary>
    private static void SeedPaymentsDemo(ModelBuilder modelBuilder)
    {
        var synced = new DateTime(2026, 10, 5, 11, 0, 0, DateTimeKind.Utc);

        CustomerInvoice Invoice(
            Guid id, Guid organizationId, string legalName, string taxId, string country, string? sii, string source,
            string type, DateOnly issue, DateOnly? due, Guid? blId, string? bl, string? booking, decimal net, decimal tax,
            string currency, string status, bool payable, string? concept = null, DateTime? paidAt = null) => new()
        {
            Id = id,
            OrganizationId = organizationId,
            SiiNumber = sii,
            SourceNumber = source,
            DocumentType = type,
            IssueDate = issue,
            DueDate = due,
            BillOfLadingId = blId,
            BlNumber = bl,
            BookingNumber = booking,
            LegalName = legalName,
            TaxId = taxId,
            NetAmount = net,
            TaxAmount = tax,
            TotalAmount = net + tax,
            Currency = currency,
            Status = status,
            SiiStatus = sii is null ? null : "ACCEPTED",
            IsPayable = payable,
            Country = country,
            ConceptCode = concept,
            PaidAt = paidAt,
            SyncedAt = synced,
            Source = "DUMMY",
            CreatedAt = synced,
            CreatedBy = "SYSTEM"
        };

        const string importadora = "Importadora Demo SpA";
        const string importadoraTaxId = "76123456-7";
        const string altiplano = "Comercial Altiplano SRL";
        const string andes = "Distribuidora Andes Crédito SpA";

        modelBuilder.Entity<CustomerInvoice>().HasData(
            Invoice(SeedDataIds.InvoiceDemurrageBL09, SeedDataIds.DemoClientCL, importadora, importadoraTaxId, CountryCodes.Chile,
                "100245", "FAC-DEM-2026-0915", InvoiceDocumentTypes.ExemptInvoice, new(2026, 9, 15), new(2026, 10, 15),
                SeedDataIds.BL09, "HLCUSAI260400910", "HLCUBKG2604091", 510000m, 0m, "CLP", InvoiceStatus.Pending, true,
                ChargeConceptCodes.Demurrage),
            Invoice(SeedDataIds.InvoiceOverdueBL01, SeedDataIds.DemoClientCL, importadora, importadoraTaxId, CountryCodes.Chile,
                "100198", "HL-CL-2026-003987", InvoiceDocumentTypes.Invoice, new(2026, 8, 20), new(2026, 9, 19),
                SeedDataIds.BL01, "HLCUVAL250100123", "HLCUBKG2501001", 120000m, 22800m, "CLP", InvoiceStatus.Pending, true),
            Invoice(SeedDataIds.InvoicePaidBL02, SeedDataIds.DemoClientCL, importadora, importadoraTaxId, CountryCodes.Chile,
                "100150", "HL-CL-2026-003501", InvoiceDocumentTypes.Invoice, new(2026, 7, 10), new(2026, 8, 9),
                SeedDataIds.BL02, "HLCUVAL250200456", "HLCUBKG2502004", 185000m, 35150m, "CLP", InvoiceStatus.Paid, false,
                paidAt: new DateTime(2026, 8, 5, 15, 0, 0, DateTimeKind.Utc)),
            Invoice(SeedDataIds.InvoiceNoFolioBL06, SeedDataIds.DemoClientCL, importadora, importadoraTaxId, CountryCodes.Chile,
                null, "HL-CL-2026-004601", InvoiceDocumentTypes.ExemptInvoice, new(2026, 10, 2), new(2026, 11, 1),
                SeedDataIds.BL06, "HLCUSAI260300610", "HLCUBKG2603061", 350m, 0m, "USD", InvoiceStatus.Pending, true),
            Invoice(SeedDataIds.InvoiceCreditNoteBL01, SeedDataIds.DemoClientCL, importadora, importadoraTaxId, CountryCodes.Chile,
                "100301", "HL-CL-2026-004700", InvoiceDocumentTypes.CreditNote, new(2026, 9, 25), null,
                SeedDataIds.BL01, "HLCUVAL250100123", "HLCUBKG2501001", 15000m, 2850m, "CLP", InvoiceStatus.Paid, false),
            Invoice(SeedDataIds.InvoiceEurBL10, SeedDataIds.DemoClientCL, importadora, importadoraTaxId, CountryCodes.Chile,
                "100260", "HL-CL-2026-004530", InvoiceDocumentTypes.ExemptInvoice, new(2026, 9, 28), new(2026, 10, 28),
                SeedDataIds.BL10, "HLCUSAI260401020", "HLCUBKG2604102", 480m, 0m, "EUR", InvoiceStatus.Pending, true),
            Invoice(SeedDataIds.InvoicePendingBO, SeedDataIds.DemoClientBO, altiplano, "1023456017", CountryCodes.Bolivia,
                "2026-000812", "HL-BO-2026-000812", InvoiceDocumentTypes.Invoice, new(2026, 9, 10), new(2026, 10, 10),
                SeedDataIds.BL04, "HLCUARI260100045", "HLCUBKG2601045", 1280m, 166.40m, "BOB", InvoiceStatus.Pending, true),
            Invoice(SeedDataIds.InvoicePaidBO, SeedDataIds.DemoClientBO, altiplano, "1023456017", CountryCodes.Bolivia,
                "2026-000790", "HL-BO-2026-000790", InvoiceDocumentTypes.Invoice, new(2026, 8, 30), new(2026, 9, 29),
                SeedDataIds.BL05, "HLCUIQQ260200078", "HLCUBKG2602078", 690m, 89.70m, "BOB", InvoiceStatus.Paid, false,
                paidAt: new DateTime(2026, 9, 5, 14, 0, 0, DateTimeKind.Utc)),
            Invoice(SeedDataIds.InvoiceCreditCustomer01, SeedDataIds.CreditDemoClient, andes, "76000002-2", CountryCodes.Chile,
                "100277", "HL-CL-2026-004588", InvoiceDocumentTypes.Invoice, new(2026, 9, 26), new(2026, 10, 26),
                SeedDataIds.BL11, "HLCUVAP260401130", "HLCUBKG2604113", 95000m, 18050m, "CLP", InvoiceStatus.Pending, true),
            Invoice(SeedDataIds.InvoiceCreditCustomer02, SeedDataIds.CreditDemoClient, andes, "76000002-2", CountryCodes.Chile,
                "100278", "HL-CL-2026-004589", InvoiceDocumentTypes.ExemptInvoice, new(2026, 9, 26), new(2026, 10, 26),
                SeedDataIds.BL11, "HLCUVAP260401130", "HLCUBKG2604113", 380m, 0m, "USD", InvoiceStatus.Pending, true));

        Payment Seeded(
            Guid id, string number, Guid clientId, string origin, string method, string? provider, string currency,
            decimal amount, decimal tax, string status, DateTime paidAt, Guid? createdBy, string payerTaxId, string payerName) => new()
        {
            Id = id,
            PaymentNumber = number,
            PaymentType = origin,
            PaymentMethod = method,
            PaymentMethodCode = method,
            ProviderKey = provider,
            Amount = amount,
            TaxAmount = tax,
            TotalAmount = amount + tax,
            Currency = currency,
            Status = status,
            StatusChangedAt = paidAt,
            Country = currency == "BOB" ? CountryCodes.Bolivia : CountryCodes.Chile,
            PaymentDate = paidAt,
            ClientId = clientId,
            Origin = origin,
            CreatedByUserId = createdBy,
            ExternalReference = number,
            PayerTaxId = payerTaxId,
            PayerName = payerName,
            CreatedAt = paidAt,
            CreatedBy = createdBy?.ToString() ?? "SYSTEM"
        };

        var khipuPaid = new DateTime(2026, 9, 12, 15, 20, 0, DateTimeKind.Utc);
        var mandatePaid = new DateTime(2026, 9, 20, 14, 5, 0, DateTimeKind.Utc);
        var slipIssued = new DateTime(2026, 10, 3, 13, 30, 0, DateTimeKind.Utc);
        var failedAt = new DateTime(2026, 10, 4, 16, 45, 0, DateTimeKind.Utc);
        var boliviaPaid = new DateTime(2026, 9, 5, 14, 0, 0, DateTimeKind.Utc);

        var payment09 = Seeded(SeedDataIds.Payment09, "PAY-20260912-1A2B3C4D", SeedDataIds.DemoClientCL, PaymentOrigins.Cart,
            PaymentMethodCodes.Khipu, PaymentProviderKeys.Khipu, "CLP", 70000m, 13300m, PaymentStatus.Confirmed, khipuPaid,
            SeedDataIds.DemoUserCL, importadoraTaxId, importadora);
        payment09.ConfirmedAt = khipuPaid;
        payment09.ConfirmedBy = "KHIPU_WEBHOOK";
        payment09.ReceiptNumber = "RCP-20260912-7F3A21C4";
        payment09.ProviderReference = "DUMMY-KHIPU-PAY-20260912-1A2B3C4D";
        payment09.ProviderTransactionId = "KHP-TXN-8812345";

        // La agencia paga el flete de BL02 bajo el mandato de Importadora Demo: pagador ≠ RUT de facturación.
        var payment10 = Seeded(SeedDataIds.Payment10, "PAY-20260920-5E6F7A8B", SeedDataIds.AgentClientCL, PaymentOrigins.Cart,
            PaymentMethodCodes.BankButtonBancoChile, PaymentProviderKeys.BancoChile, "CLP", 4940000m, 0m, PaymentStatus.Confirmed,
            mandatePaid, SeedDataIds.AgentUserCL, "96555444-3", "Agencia Marítima del Pacífico Ltda");
        payment10.ConfirmedAt = mandatePaid;
        payment10.ConfirmedBy = "BANCOCHILE_WEBHOOK";
        payment10.ReceiptNumber = "RCP-20260920-2B4D6F80";
        payment10.ProviderReference = "DUMMY-BANCOCHILE-PAY-20260920-5E6F7A8B";
        payment10.ProviderTransactionId = "BCH-TXN-55100231";
        payment10.ExchangeRate = 950m;
        payment10.BillOfLadingId = SeedDataIds.BL02;
        payment10.OnBehalfOfClientId = SeedDataIds.DemoClientCL;
        payment10.AccessGrantId = SeedDataIds.DemoAccessGrant02;

        // Boleta de depósito emitida: el cliente ya no puede anularla (M5-02); Finanzas verifica el abono.
        var payment11 = Seeded(SeedDataIds.Payment11, "PAY-20261003-9C8B7A6D", SeedDataIds.DemoClientCL, PaymentOrigins.Cart,
            PaymentMethodCodes.Deposit, null, "CLP", 45000m, 8550m, PaymentStatus.PendingVerification, slipIssued,
            SeedDataIds.DemoUserCL, importadoraTaxId, importadora);
        payment11.SlipNumber = "BDP-20261003-5C7D9E1F";
        payment11.SlipIssuedAt = slipIssued;
        payment11.BillOfLadingId = SeedDataIds.BL06;

        var payment12 = Seeded(SeedDataIds.Payment12, "PAY-20261004-3D2C1B0A", SeedDataIds.DemoClientCL, PaymentOrigins.Cart,
            PaymentMethodCodes.Khipu, PaymentProviderKeys.Khipu, "CLP", 85000m, 16150m, PaymentStatus.Failed, failedAt,
            SeedDataIds.DemoUserCL, importadoraTaxId, importadora);
        payment12.FailureReason = PaymentFailureReasons.ProviderUnavailable;
        payment12.BillOfLadingId = SeedDataIds.BL09;

        var payment13 = Seeded(SeedDataIds.Payment13, "PAY-20260903-4A5B6C7D", SeedDataIds.DemoClientBO, PaymentOrigins.Cart,
            PaymentMethodCodes.Deposit, null, "BOB", 820m, 106.60m, PaymentStatus.Confirmed, boliviaPaid,
            SeedDataIds.DemoUserBO, "1023456017", altiplano);
        payment13.ConfirmedAt = boliviaPaid;
        payment13.ConfirmedBy = "admin@hapag-lloyd.cl";
        payment13.ReceiptNumber = "RCP-20260905-8E9F0A1B";
        payment13.SlipNumber = "BDP-20260903-1F2E3D4C";
        payment13.SlipIssuedAt = boliviaPaid.AddDays(-2);

        modelBuilder.Entity<Payment>().HasData(payment09, payment10, payment11, payment12, payment13);

        PaymentDetail Detail(
            Guid paymentId, int line, string itemType, Guid? sourceId, string concept, string description, Guid? blId, string? bl,
            string? booking, decimal amount, decimal tax, string currency, string billingTaxId, string billingName,
            decimal? originalAmount = null, string? originalCurrency = null, decimal? rate = null, DateTime? releasedAt = null) => new()
        {
            Id = DeterministicGuid($"payment-detail:{paymentId}:{line}"),
            PaymentId = paymentId,
            ConceptType = concept,
            Description = description,
            Amount = amount,
            TaxAmount = tax,
            Currency = currency,
            ItemType = itemType,
            SourceId = sourceId,
            BillOfLadingId = blId,
            BlNumber = bl,
            BookingNumber = booking,
            BillingTaxId = billingTaxId,
            BillingName = billingName,
            OriginalAmount = originalAmount ?? amount + tax,
            OriginalCurrency = originalCurrency ?? currency,
            ExchangeRate = rate,
            ReleasedAt = releasedAt
        };

        modelBuilder.Entity<PaymentDetail>().HasData(
            // Historial: recargos de pagos anteriores, sin fuente vigente que liberar.
            Detail(SeedDataIds.Payment09, 1, PayableItemTypes.LocalCharge, null, ChargeConceptCodes.Isps, "ISPS - histórico",
                SeedDataIds.BL02, "HLCUVAL250200456", "HLCUBKG2502004", 25000m, 4750m, "CLP", importadoraTaxId, importadora,
                releasedAt: khipuPaid),
            Detail(SeedDataIds.Payment09, 2, PayableItemTypes.LocalCharge, null, ChargeConceptCodes.BlFee, "Emisión de BL - histórico",
                SeedDataIds.BL06, "HLCUSAI260300610", "HLCUBKG2603061", 45000m, 8550m, "CLP", importadoraTaxId, importadora,
                releasedAt: khipuPaid),
            Detail(SeedDataIds.Payment10, 1, PayableItemTypes.Freight, SeedDataIds.BL02, PaymentConcepts.Freight, "Flete Busan - Valparaíso",
                SeedDataIds.BL02, "HLCUVAL250200456", "HLCUBKG2502004", 4940000m, 0m, "CLP", importadoraTaxId, importadora,
                5200m, "USD", 950m, mandatePaid),
            Detail(SeedDataIds.Payment11, 1, PayableItemTypes.LocalCharge, SeedDataIds.LocalCharge10, ChargeConceptCodes.BlFee,
                "BL Documentation Fee (export)", SeedDataIds.BL06, "HLCUSAI260300610", "HLCUBKG2603061", 45000m, 8550m, "CLP",
                importadoraTaxId, importadora),
            Detail(SeedDataIds.Payment12, 1, PayableItemTypes.LocalCharge, SeedDataIds.LocalCharge13, ChargeConceptCodes.Mhd,
                "MHD - HLXU3034001", SeedDataIds.BL09, "HLCUSAI260400910", "HLCUBKG2604091", 85000m, 16150m, "CLP",
                importadoraTaxId, importadora),
            Detail(SeedDataIds.Payment13, 1, PayableItemTypes.LocalCharge, null, ChargeConceptCodes.GateIn, "Gate In - histórico",
                SeedDataIds.BL05, "HLCUIQQ260200078", "HLCUBKG2602078", 820m, 106.60m, "BOB", "1023456017", altiplano,
                releasedAt: boliviaPaid));

        var history = new (Guid PaymentId, int Order, string? From, string To, DateTime At, string By, Guid? UserId, string? Reason)[]
        {
            (SeedDataIds.Payment09, 1, null, PaymentStatus.Pending, khipuPaid.AddMinutes(-2), "demo@importadorademo.cl", SeedDataIds.DemoUserCL, null),
            (SeedDataIds.Payment09, 2, PaymentStatus.Pending, PaymentStatus.Processing, khipuPaid.AddMinutes(-2), "SYSTEM", null, "Initiated in Khipu"),
            (SeedDataIds.Payment09, 3, PaymentStatus.Processing, PaymentStatus.Confirmed, khipuPaid, "KHIPU_WEBHOOK", null, null),
            (SeedDataIds.Payment10, 1, null, PaymentStatus.Pending, mandatePaid.AddMinutes(-3), "agente@maritimpacifico.cl", SeedDataIds.AgentUserCL, null),
            (SeedDataIds.Payment10, 2, PaymentStatus.Pending, PaymentStatus.Processing, mandatePaid.AddMinutes(-3), "SYSTEM", null, "Initiated in BancoChile"),
            (SeedDataIds.Payment10, 3, PaymentStatus.Processing, PaymentStatus.Confirmed, mandatePaid, "BANCOCHILE_WEBHOOK", null, null),
            (SeedDataIds.Payment11, 1, null, PaymentStatus.Pending, slipIssued.AddMinutes(-10), "demo@importadorademo.cl", SeedDataIds.DemoUserCL, null),
            (SeedDataIds.Payment11, 2, PaymentStatus.Pending, PaymentStatus.PendingVerification, slipIssued, "demo@importadorademo.cl", SeedDataIds.DemoUserCL, "Deposit slip issued"),
            (SeedDataIds.Payment12, 1, null, PaymentStatus.Pending, failedAt.AddSeconds(-30), "demo@importadorademo.cl", SeedDataIds.DemoUserCL, null),
            (SeedDataIds.Payment12, 2, PaymentStatus.Pending, PaymentStatus.Failed, failedAt, "SYSTEM", null, PaymentFailureReasons.ProviderUnavailable),
            (SeedDataIds.Payment13, 1, null, PaymentStatus.Pending, boliviaPaid.AddDays(-2).AddMinutes(-5), "demo@altiplano.bo", SeedDataIds.DemoUserBO, null),
            (SeedDataIds.Payment13, 2, PaymentStatus.Pending, PaymentStatus.PendingVerification, boliviaPaid.AddDays(-2), "demo@altiplano.bo", SeedDataIds.DemoUserBO, "Deposit slip issued"),
            (SeedDataIds.Payment13, 3, PaymentStatus.PendingVerification, PaymentStatus.Confirmed, boliviaPaid, "admin@hapag-lloyd.cl", SeedDataIds.AdminUser, null),
        };

        modelBuilder.Entity<PaymentStatusChange>().HasData(history.Select(h => new PaymentStatusChange
        {
            Id = DeterministicGuid($"payment-status:{h.PaymentId}:{h.Order}"),
            PaymentId = h.PaymentId,
            FromStatus = h.From,
            ToStatus = h.To,
            ChangedAt = h.At,
            ChangedBy = h.By,
            ChangedByUserId = h.UserId,
            Reason = h.Reason
        }));

        // M5-05: tipo de cambio y vigencia usados en la conversión del pago bajo mandato.
        modelBuilder.Entity<ExchangeRateRecord>().HasData(new ExchangeRateRecord
        {
            Id = DeterministicGuid($"exchange-rate:payment:{SeedDataIds.Payment10}"),
            TransactionType = ExchangeRateTransactionTypes.Payment,
            TransactionId = SeedDataIds.Payment10,
            FromCurrency = "USD",
            ToCurrency = "CLP",
            Rate = 950m,
            EffectiveDate = new DateOnly(2026, 9, 20),
            Source = "DUMMY",
            Approved = true,
            SourceAmount = 5200m,
            ConvertedAmount = 4940000m,
            CapturedAt = mandatePaid.AddMinutes(-3)
        });
    }

    /// <summary>
    /// Fase 1 Ola C: catálogo de conceptos de cobro (coherente con los códigos de los recargos sembrados),
    /// tarifas del mantenedor (M8-01) con su alta en el registro de cambios (NF-15), reglas internas
    /// (M3-04, M3-16) y feriados de referencia del calendario de negocio (NF-22, por validar con Hapag).
    /// </summary>
    private static void SeedChargeCatalogAndTariffs(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var tariffsCreated = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

        // (Code, Name, Category, Countries, NexusTariff, NexusExemptible)
        var concepts = new (string Code, string Name, string Category, string Countries, bool NexusTariff, bool NexusExemptible)[]
        {
            (ChargeConceptCodes.GateIn, "Gate In", ChargeCategories.LocalCharge, "CL,BO", false, true),
            (ChargeConceptCodes.Eds, "EDS", ChargeCategories.LocalCharge, "CL,BO", false, true),
            (ChargeConceptCodes.GateOut, "Gate Out", ChargeCategories.LocalCharge, "CL,BO", false, true),
            (ChargeConceptCodes.Ipo, "Recargo IPO", ChargeCategories.LocalCharge, "CL,BO", true, false),
            (ChargeConceptCodes.Thc, "Terminal Handling Charge", ChargeCategories.LocalCharge, "CL,BO", false, false),
            (ChargeConceptCodes.ThcReefer, "Terminal Handling Charge Reefer", ChargeCategories.LocalCharge, "CL,BO", false, false),
            (ChargeConceptCodes.BlFee, "Emisión de BL", ChargeCategories.LocalCharge, "CL,BO", false, false),
            (ChargeConceptCodes.Isps, "Recargo de seguridad ISPS", ChargeCategories.LocalCharge, "CL,BO", false, false),
            (ChargeConceptCodes.TransitFee, "Documentación de tránsito Bolivia", ChargeCategories.LocalCharge, "BO", false, false),
            (ChargeConceptCodes.Mhd, "MHD", ChargeCategories.Demurrage, "CL,BO", false, false),
            (ChargeConceptCodes.Demurrage, "Demurrage (sobreestadía)", ChargeCategories.Demurrage, "CL,BO", false, false),
            (ChargeConceptCodes.AdvanceDemurrageBo, "Demoras anticipadas", ChargeCategories.Demurrage, "BO", false, false),
            (ChargeConceptCodes.WarehouseChange, "Cambio de almacén", ChargeCategories.Service, "CL,BO", true, false),
            (ChargeConceptCodes.LateArrival, "Late Arrival", ChargeCategories.Service, "CL", false, false),
            (ChargeConceptCodes.TransshipmentCertificate, "Certificado de transbordo", ChargeCategories.Service, "CL", false, false),
        };

        modelBuilder.Entity<ChargeConcept>().HasData(concepts.Select((c, index) => new ChargeConcept
        {
            Id = DeterministicGuid($"charge-concept:{c.Code}"),
            Code = c.Code,
            Name = c.Name,
            Category = c.Category,
            Countries = c.Countries,
            NexusTariff = c.NexusTariff,
            NexusExemptible = c.NexusExemptible,
            DisplayOrder = (index + 1) * 10,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        }));

        var october = new DateOnly(2026, 10, 1);
        var january = new DateOnly(2026, 1, 1);

        var tariffs = new[]
        {
            new Tariff { Id = SeedDataIds.TariffKteCL, ConceptCode = ChargeConceptCodes.WarehouseChange, Code = "KTE", Country = CountryCodes.Chile, Currency = "CLP", Description = "Cambio de almacén (KTE)", Amount = 9940m, ValidFrom = october },
            new Tariff { Id = SeedDataIds.TariffKtfCL, ConceptCode = ChargeConceptCodes.WarehouseChange, Code = "KTF", Country = CountryCodes.Chile, Currency = "CLP", Description = "Cambio de almacén (KTF)", Amount = 110910m, ValidFrom = october },
            new Tariff { Id = SeedDataIds.TariffWarehouseChangeBO, ConceptCode = ChargeConceptCodes.WarehouseChange, Country = CountryCodes.Bolivia, Currency = "BOB", Description = "Cambio de almacén Bolivia", Amount = 850m, ValidFrom = january },
            new Tariff { Id = SeedDataIds.TariffLateArrivalCL, ConceptCode = ChargeConceptCodes.LateArrival, Country = CountryCodes.Chile, Currency = "USD", Description = "Late Arrival por horas desde el cierre de recepción", TierUnit = TariffTierUnits.Hours, TierMode = TariffTierModes.Flat, ValidFrom = october },
            new Tariff { Id = SeedDataIds.TariffDemurrageCL20, ConceptCode = ChargeConceptCodes.Demurrage, Country = CountryCodes.Chile, Currency = "CLP", ContainerType = "20DV", Description = "Demurrage 20' por día desde la descarga", TierUnit = TariffTierUnits.CalendarDays, TierMode = TariffTierModes.PerUnit, ValidFrom = january },
            new Tariff { Id = SeedDataIds.TariffDemurrageCL, ConceptCode = ChargeConceptCodes.Demurrage, Country = CountryCodes.Chile, Currency = "CLP", Description = "Demurrage 40' y especiales por día desde la descarga", TierUnit = TariffTierUnits.CalendarDays, TierMode = TariffTierModes.PerUnit, ValidFrom = january },
            new Tariff { Id = SeedDataIds.TariffDemurrageBO, ConceptCode = ChargeConceptCodes.Demurrage, Country = CountryCodes.Bolivia, Currency = "BOB", Description = "Demurrage Bolivia por día desde la descarga", TierUnit = TariffTierUnits.CalendarDays, TierMode = TariffTierModes.PerUnit, ValidFrom = january },
            new Tariff { Id = SeedDataIds.TariffAdvanceDemurrageBO, ConceptCode = ChargeConceptCodes.AdvanceDemurrageBo, Country = CountryCodes.Bolivia, Currency = "USD", Description = "Demoras anticipadas por contenedor", Amount = 150m, ValidFrom = january },
            new Tariff { Id = SeedDataIds.TariffTransshipmentCL, ConceptCode = ChargeConceptCodes.TransshipmentCertificate, Country = CountryCodes.Chile, Currency = "CLP", Description = "Certificado de transbordo (M6-01)", Amount = 35000m, ValidFrom = october },
        };

        var tiers = new (Guid TariffId, int From, int? To, decimal Amount)[]
        {
            (SeedDataIds.TariffLateArrivalCL, 0, 24, 100m),
            (SeedDataIds.TariffLateArrivalCL, 25, 48, 200m),
            (SeedDataIds.TariffLateArrivalCL, 49, null, 350m),
            (SeedDataIds.TariffDemurrageCL20, 1, 7, 0m),
            (SeedDataIds.TariffDemurrageCL20, 8, 14, 35000m),
            (SeedDataIds.TariffDemurrageCL20, 15, null, 50000m),
            (SeedDataIds.TariffDemurrageCL, 1, 7, 0m),
            (SeedDataIds.TariffDemurrageCL, 8, 14, 45000m),
            (SeedDataIds.TariffDemurrageCL, 15, null, 65000m),
            (SeedDataIds.TariffDemurrageBO, 1, 10, 0m),
            (SeedDataIds.TariffDemurrageBO, 11, 20, 310m),
            (SeedDataIds.TariffDemurrageBO, 21, null, 450m),
        };

        var tierEntities = tiers.Select(t => new TariffTier
        {
            Id = DeterministicGuid($"tariff-tier:{t.TariffId}:{t.From}"),
            TariffId = t.TariffId,
            FromUnit = t.From,
            ToUnit = t.To,
            Amount = t.Amount
        }).ToList();

        foreach (var tariff in tariffs)
        {
            tariff.CreatedAt = tariffsCreated;
            tariff.CreatedBy = "SYSTEM";
        }

        modelBuilder.Entity<Tariff>().HasData(tariffs);
        modelBuilder.Entity<TariffTier>().HasData(tierEntities);

        // NF-15: el alta de cada tarifa sembrada queda en el registro de cambios para reconstruirla.
        modelBuilder.Entity<MaintainerChangeLog>().HasData(tariffs.Select(t =>
        {
            var snapshot = TariffSnapshot.From(new Tariff
            {
                ConceptCode = t.ConceptCode,
                Code = t.Code,
                Country = t.Country,
                Currency = t.Currency,
                ContainerType = t.ContainerType,
                Description = t.Description,
                Amount = t.Amount,
                TierUnit = t.TierUnit,
                TierMode = t.TierMode,
                ValidFrom = t.ValidFrom,
                ValidTo = t.ValidTo,
                IsActive = t.IsActive,
                Tiers = tierEntities.Where(x => x.TariffId == t.Id).ToList()
            });

            return new MaintainerChangeLog
            {
                Id = DeterministicGuid($"maintainer-log:tariff:{t.Id}"),
                Maintainer = MaintainerNames.Tariff,
                EntityId = t.Id,
                Action = MaintainerActions.Created,
                NewValue = JsonSerializer.Serialize(snapshot, MaintainerChangeLogger.JsonOptions),
                ChangedAt = tariffsCreated,
                ChangedBy = "SYSTEM"
            };
        }));

        var rules = new[]
        {
            new InternalChargeRule
            {
                Id = SeedDataIds.RuleFreeWarehouseChangeCL,
                RuleType = InternalChargeRuleTypes.FreeWarehouseChange,
                Country = CountryCodes.Chile,
                TaxId = "76123456-7",
                MatchCode = "MC100010",
                AccountName = "Importadora Demo SpA",
                Reason = "Convenio comercial: un cambio de almacén gratuito por BL",
                MaxUsesPerBl = 1,
                ValidFrom = january,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new InternalChargeRule
            {
                Id = SeedDataIds.RuleAdvanceDemurrageBO,
                RuleType = InternalChargeRuleTypes.AdvanceDemurrageRequired,
                Country = CountryCodes.Bolivia,
                TaxId = "1023456017",
                MatchCode = "MC100020",
                AccountName = "Comercial Altiplano SRL",
                Reason = "Regla interna Bolivia: demoras anticipadas obligatorias antes del CLD",
                ValidFrom = january,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
        };

        modelBuilder.Entity<InternalChargeRule>().HasData(rules);
        modelBuilder.Entity<MaintainerChangeLog>().HasData(rules.Select(r => new MaintainerChangeLog
        {
            Id = DeterministicGuid($"maintainer-log:rule:{r.Id}"),
            Maintainer = MaintainerNames.InternalChargeRule,
            EntityId = r.Id,
            Action = MaintainerActions.Created,
            NewValue = JsonSerializer.Serialize(InternalChargeRuleSnapshot.From(r), MaintainerChangeLogger.JsonOptions),
            ChangedAt = now,
            ChangedBy = "SYSTEM"
        }));

        // Calendario de negocio de referencia (NF-22): feriados nacionales 2026; Hapag-Lloyd publica el oficial.
        var holidays = new (string Country, DateOnly Date, string Name)[]
        {
            (CountryCodes.Chile, new(2026, 1, 1), "Año Nuevo"),
            (CountryCodes.Chile, new(2026, 4, 3), "Viernes Santo"),
            (CountryCodes.Chile, new(2026, 4, 4), "Sábado Santo"),
            (CountryCodes.Chile, new(2026, 5, 1), "Día del Trabajo"),
            (CountryCodes.Chile, new(2026, 5, 21), "Día de las Glorias Navales"),
            (CountryCodes.Chile, new(2026, 6, 29), "San Pedro y San Pablo"),
            (CountryCodes.Chile, new(2026, 7, 16), "Virgen del Carmen"),
            (CountryCodes.Chile, new(2026, 8, 15), "Asunción de la Virgen"),
            (CountryCodes.Chile, new(2026, 9, 18), "Independencia Nacional"),
            (CountryCodes.Chile, new(2026, 9, 19), "Glorias del Ejército"),
            (CountryCodes.Chile, new(2026, 10, 12), "Encuentro de Dos Mundos"),
            (CountryCodes.Chile, new(2026, 10, 31), "Día de las Iglesias Evangélicas"),
            (CountryCodes.Chile, new(2026, 11, 1), "Día de Todos los Santos"),
            (CountryCodes.Chile, new(2026, 12, 8), "Inmaculada Concepción"),
            (CountryCodes.Chile, new(2026, 12, 25), "Navidad"),
            (CountryCodes.Bolivia, new(2026, 1, 1), "Año Nuevo"),
            (CountryCodes.Bolivia, new(2026, 1, 22), "Día del Estado Plurinacional"),
            (CountryCodes.Bolivia, new(2026, 2, 16), "Carnaval"),
            (CountryCodes.Bolivia, new(2026, 2, 17), "Carnaval"),
            (CountryCodes.Bolivia, new(2026, 4, 3), "Viernes Santo"),
            (CountryCodes.Bolivia, new(2026, 5, 1), "Día del Trabajo"),
            (CountryCodes.Bolivia, new(2026, 6, 4), "Corpus Christi"),
            (CountryCodes.Bolivia, new(2026, 6, 21), "Año Nuevo Andino Amazónico"),
            (CountryCodes.Bolivia, new(2026, 8, 6), "Día de la Independencia"),
            (CountryCodes.Bolivia, new(2026, 11, 2), "Día de Todos los Difuntos"),
            (CountryCodes.Bolivia, new(2026, 12, 25), "Navidad"),
        };

        modelBuilder.Entity<BusinessHoliday>().HasData(holidays.Select(h => new BusinessHoliday
        {
            Id = DeterministicGuid($"holiday:{h.Country}:{h.Date:yyyy-MM-dd}"),
            Country = h.Country,
            Date = h.Date,
            Name = h.Name
        }));
    }

    /// <summary>
    /// Datos de demostración de Fase 1 Ola C sobre los escenarios del Dummy de Nexus: un BL con Gate In y
    /// EDS exentos por el consignatario del Master (76000001-1, M4-02), un cliente con crédito
    /// (76000002-2) cuyo IPO no se presenta (M4-03), un FFWW (76000003-3) que requiere carta (M4-04),
    /// MHD en BL de Chile y Bolivia (M3-02), un BL con demurrage facturado (M3-18), otro sin calcular, y
    /// la cuenta boliviana sujeta a demoras anticipadas (M3-16).
    /// </summary>
    private static void SeedChargeRulesDemo(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // BCrypt hash of "Admin123!" with work factor 12 (reused for all demo users)
        const string demoPasswordHash = "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca";

        modelBuilder.Entity<Client>().HasData(
            new Client
            {
                Id = SeedDataIds.CreditDemoClient,
                Name = "Distribuidora Andes Crédito SpA",
                TaxId = "76.000.002-2",
                TaxIdType = "RUT",
                Country = CountryCodes.Chile,
                Email = "contacto@distribuidoraandes.cl",
                ClientType = "Client",
                OrganizationType = OrganizationTypes.Customer,
                RegistrationStatus = OrganizationStatus.Approved,
                MatchCode = "MC000202",
                OperatingCountries = "CL",
                ApprovedAt = now,
                IsActive = true,
                IsEmailConfirmed = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new Client
            {
                Id = SeedDataIds.FfwwDemoClient,
                Name = "Global Forwarding Chile SpA",
                TaxId = "76.000.003-3",
                TaxIdType = "RUT",
                Country = CountryCodes.Chile,
                Email = "operaciones@globalforwarding.cl",
                ClientType = "Client",
                OrganizationType = OrganizationTypes.FreightForwarder,
                RegistrationStatus = OrganizationStatus.Approved,
                MatchCode = "MC000303",
                OperatingCountries = "CL",
                ApprovedAt = now,
                IsActive = true,
                IsEmailConfirmed = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = SeedDataIds.CreditDemoUser,
                Username = "credito@distribuidoraandes.cl",
                Email = "credito@distribuidoraandes.cl",
                PasswordHash = demoPasswordHash,
                UserType = "Client",
                Country = CountryCodes.Chile,
                FirstName = "Camila",
                LastName = "Crédito",
                IsActive = true,
                MembershipStatus = MembershipStatus.Active,
                ClientId = SeedDataIds.CreditDemoClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new User
            {
                Id = SeedDataIds.FfwwDemoUser,
                Username = "ffww@globalforwarding.cl",
                Email = "ffww@globalforwarding.cl",
                PasswordHash = demoPasswordHash,
                UserType = "Client",
                Country = CountryCodes.Chile,
                FirstName = "Felipe",
                LastName = "Forwarder",
                IsActive = true,
                MembershipStatus = MembershipStatus.Active,
                ClientId = SeedDataIds.FfwwDemoClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        modelBuilder.Entity<UserRole>().HasData(
            new[] { SeedDataIds.CreditDemoUser, SeedDataIds.FfwwDemoUser }.Select(userId => new UserRole
            {
                Id = DeterministicGuid($"user-profile:{userId}"),
                UserId = userId,
                RoleName = RoleCodes.OrgAdmin,
                RoleId = DeterministicGuid($"role:{RoleCodes.OrgAdmin}")
            }));

        modelBuilder.Entity<BillOfLading>().HasData(
            // CL importación con demurrage facturado y deuda vigente (M3-18: pagar la factura).
            new BillOfLading
            {
                Id = SeedDataIds.BL09,
                BLNumber = "HLCUSAI260400910",
                BookingNumber = "HLCUBKG2604091",
                ShipmentType = "Import",
                Vessel = "Rio de Janeiro Express",
                Voyage = "2608E",
                PortOfLoading = "Hamburg (DEHAM)",
                PortOfDischarge = "San Antonio (CLSAI)",
                PlaceOfDelivery = "Santiago, Chile",
                ETD = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                ETA = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                Consignee = "Importadora Demo SpA",
                Shipper = "Hamburg Industrial Supplies GmbH",
                FreightAmount = 4200m,
                FreightCurrency = "USD",
                Status = "Arrived",
                Country = CountryCodes.Chile,
                ClientId = SeedDataIds.DemoClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // CL importación (Master) con Gate In y EDS exentos por el consignatario del Master y demurrage sin calcular.
            new BillOfLading
            {
                Id = SeedDataIds.BL10,
                BLNumber = "HLCUSAI260401020",
                BookingNumber = "HLCUBKG2604102",
                ShipmentType = "Import",
                Vessel = "Valparaiso Express",
                Voyage = "2609E",
                PortOfLoading = "Ningbo (CNNGB)",
                PortOfDischarge = "San Antonio (CLSAI)",
                PlaceOfDelivery = "Santiago, Chile",
                ETD = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                ETA = new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc),
                Consignee = "Delfin Logística SpA",
                Shipper = "Ningbo Home Goods Co.",
                FreightAmount = 3100m,
                FreightCurrency = "USD",
                Status = "Arrived",
                Country = CountryCodes.Chile,
                ClientId = SeedDataIds.DemoClientCL,
                BLType = "Master",
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // CL importación de un cliente con crédito (IPO excluido); el FFWW figura como consignatario.
            new BillOfLading
            {
                Id = SeedDataIds.BL11,
                BLNumber = "HLCUVAP260401130",
                BookingNumber = "HLCUBKG2604113",
                ShipmentType = "Import",
                Vessel = "Santos Express",
                Voyage = "2609N",
                PortOfLoading = "Santos (BRSSZ)",
                PortOfDischarge = "Valparaiso (CLVAP)",
                PlaceOfDelivery = "Valparaiso, Chile",
                ETD = new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc),
                ETA = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                Consignee = "Global Forwarding Chile SpA",
                Shipper = "Santos Coffee Exporters Ltda",
                FreightAmount = 2600m,
                FreightCurrency = "USD",
                Status = "Arrived",
                Country = CountryCodes.Chile,
                ClientId = SeedDataIds.CreditDemoClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        modelBuilder.Entity<BLParty>().HasData(new BLParty
        {
            Id = SeedDataIds.BL10MasterConsignee,
            BillOfLadingId = SeedDataIds.BL10,
            Role = "Consignee",
            Name = "Delfin Logística SpA",
            TaxId = "76000001-1",
            TaxIdType = "RUT",
            CountryCode = CountryCodes.Chile,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<BLContainer>().HasData(
            new BLContainer { Id = SeedDataIds.Container11, ContainerNumber = "HLXU3034001", ContainerType = "40HC", SealNumber = "SL-030401", Weight = 23900m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL09, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container12, ContainerNumber = "HLXU3034002", ContainerType = "20DV", SealNumber = "SL-030402", Weight = 16800m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL10, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container13, ContainerNumber = "HLXU3034003", ContainerType = "40HC", SealNumber = "SL-030403", Weight = 24100m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL10, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container14, ContainerNumber = "HLXU3034004", ContainerType = "20DV", SealNumber = "SL-030404", Weight = 17200m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL11, CreatedAt = now, CreatedBy = "SYSTEM" });

        modelBuilder.Entity<LocalCharge>().HasData(
            new LocalCharge { Id = SeedDataIds.LocalCharge13, ChargeType = ChargeConceptCodes.Mhd, Description = "MHD - HLXU3034001", Amount = 85000m, Currency = "CLP", Status = ChargeStatus.Pending, IsTaxable = true, TaxRate = 19m, TaxAmount = 16150m, TotalAmount = 101150m, BillOfLadingId = SeedDataIds.BL09, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge14, ChargeType = ChargeConceptCodes.GateIn, Description = "Gate In - devolución de vacíos (San Antonio)", Amount = 95000m, Currency = "CLP", Status = ChargeStatus.Pending, IsTaxable = true, TaxRate = 19m, TaxAmount = 18050m, TotalAmount = 113050m, BillOfLadingId = SeedDataIds.BL10, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge15, ChargeType = ChargeConceptCodes.Eds, Description = "EDS", Amount = 38000m, Currency = "CLP", Status = ChargeStatus.Pending, IsTaxable = true, TaxRate = 19m, TaxAmount = 7220m, TotalAmount = 45220m, BillOfLadingId = SeedDataIds.BL10, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge16, ChargeType = ChargeConceptCodes.Ipo, Description = "Recargo IPO", Amount = 150m, Currency = "USD", Status = ChargeStatus.Pending, IsTaxable = false, TaxRate = 0m, TaxAmount = 0m, TotalAmount = 150m, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge17, ChargeType = ChargeConceptCodes.Mhd, Description = "MHD - HLXU1234567 / HLXU7654321", Amount = 85000m, Currency = "CLP", Status = ChargeStatus.Pending, IsTaxable = true, TaxRate = 19m, TaxAmount = 16150m, TotalAmount = 101150m, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge18, ChargeType = ChargeConceptCodes.Thc, Description = "Terminal Handling Charge - 20DV", Amount = 185000m, Currency = "CLP", Status = ChargeStatus.Pending, IsTaxable = true, TaxRate = 19m, TaxAmount = 35150m, TotalAmount = 220150m, BillOfLadingId = SeedDataIds.BL11, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge19, ChargeType = ChargeConceptCodes.Ipo, Description = "Recargo IPO", Amount = 150m, Currency = "USD", Status = ChargeStatus.Pending, IsTaxable = false, TaxRate = 0m, TaxAmount = 0m, TotalAmount = 150m, BillOfLadingId = SeedDataIds.BL11, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge20, ChargeType = ChargeConceptCodes.GateOut, Description = "Gate Out - 20DV (Valparaíso)", Amount = 60000m, Currency = "CLP", Status = ChargeStatus.Pending, IsTaxable = true, TaxRate = 19m, TaxAmount = 11400m, TotalAmount = 71400m, BillOfLadingId = SeedDataIds.BL11, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge21, ChargeType = ChargeConceptCodes.Mhd, Description = "MHD - HLXU8899001", Amount = 450m, Currency = "USD", Status = ChargeStatus.Pending, IsTaxable = false, TaxRate = 0m, TaxAmount = 0m, TotalAmount = 450m, BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge22, ChargeType = ChargeConceptCodes.GateOut, Description = "Gate Out - 40RF (San Antonio)", Amount = 60000m, Currency = "CLP", Status = ChargeStatus.Pending, IsTaxable = true, TaxRate = 19m, TaxAmount = 11400m, TotalAmount = 71400m, BillOfLadingId = SeedDataIds.BL06, CreatedAt = now, CreatedBy = "SYSTEM" });

        // 17 días desde la descarga, 7 libres: 7 días a 45.000 y 3 a 65.000 (tarifa DEMURRAGE CL).
        modelBuilder.Entity<DemurrageCharge>().HasData(new DemurrageCharge
        {
            Id = SeedDataIds.Demurrage04,
            ContainerNumber = "HLXU3034001",
            FreeDays = 7,
            DemurrageDays = 10,
            DailyRate = 51000m,
            TotalAmount = 510000m,
            Currency = "CLP",
            StartDate = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc),
            Status = DemurrageChargeStatus.Invoiced,
            InvoiceNumber = "FAC-DEM-2026-0915",
            InvoicedAt = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc),
            InvoiceDueDate = new DateTime(2026, 10, 15, 3, 0, 0, DateTimeKind.Utc),
            IsExempt = false,
            BillOfLadingId = SeedDataIds.BL09,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        var shipmentRoles = new (Guid BlId, Guid ClientId, string Role)[]
        {
            (SeedDataIds.BL09, SeedDataIds.DemoClientCL, ShipmentRoleCodes.Consignee),
            (SeedDataIds.BL10, SeedDataIds.DemoClientCL, ShipmentRoleCodes.Consignee),
            (SeedDataIds.BL11, SeedDataIds.FfwwDemoClient, ShipmentRoleCodes.Consignee),
        };

        modelBuilder.Entity<ShipmentRole>().HasData(shipmentRoles.Select(r => new ShipmentRole
        {
            Id = DeterministicGuid($"shipment-role:{r.BlId}:{r.ClientId}:{r.Role}"),
            BillOfLadingId = r.BlId,
            ClientId = r.ClientId,
            Role = r.Role,
            Source = ShipmentRoleSources.Seed,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        }));
    }

    /// <summary>
    /// Reglas de plazo aduaneras como CONFIGURACIÓN editable (no hardcode). Valores, fuente y
    /// certeza según la tabla de la Fase 4 del plan; lo no confirmado queda marcado como tal.
    /// </summary>
    private static void SeedDeadlineRules(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // (Code, Name, BaseEvent, OffsetHours, AtRisk, Direction, Country, BLType, Severity, Source, Certainty)
        var rules = new[]
        {
            ("MANIFEST_HEADER_IN", "Encabezado manifiesto (ingreso)", DeadlineBaseEvents.ArrivalEstimated, -168, 48, "Ingreso", (string?)null, (string?)null, DeadlineSeverity.High, "Ficha aduana.cl", DeadlineCertainty.Confirmed),
            ("BL_MASTER_IN", "B/L Máster (ingreso)", DeadlineBaseEvents.ArrivalEstimated, -48, 12, "Ingreso", null, "Master", DeadlineSeverity.High, "Material oficial Aduana (verificar Res. 7591/2012)", DeadlineCertainty.ToVerify),
            ("BL_HOUSE_IN", "B/L Hijo (ingreso)", DeadlineBaseEvents.ArrivalEstimated, -24, 6, "Ingreso", null, "House", DeadlineSeverity.High, "Material oficial Aduana", DeadlineCertainty.ToVerify),
            ("MANIFEST_AMEND_IN", "Aclaración al manifiesto (ingreso)", DeadlineBaseEvents.DepartureEstimated, 168, 48, "Ingreso", null, null, DeadlineSeverity.Medium, "Cap. 3 CNA num. 2.6", DeadlineCertainty.Confirmed),
            ("GOODS_DELIVERY", "Entrega de mercancías a almacenista", DeadlineBaseEvents.DepartureEstimated, 24, 6, null, null, null, DeadlineSeverity.Medium, "Cap. 3 CNA num. 2.4", DeadlineCertainty.Confirmed),
            ("MANIFEST_HEADER_OUT", "Encabezado manifiesto (salida)", DeadlineBaseEvents.DepartureEstimated, -48, 12, "Salida", null, null, DeadlineSeverity.High, "Res. 9432/2008", DeadlineCertainty.Confirmed),
            ("BL_EMPTY_OUT", "B/L y contenedores vacíos (salida)", DeadlineBaseEvents.DepartureEstimated, 72, 24, "Salida", null, null, DeadlineSeverity.Low, "Res. 6609/2012 (valor por confirmar)", DeadlineCertainty.Uncertain),
            ("MICDTA_BO", "MIC/DTA tránsito (Bolivia)", DeadlineBaseEvents.DespatchRequest, 48, 12, null, "BO", null, DeadlineSeverity.Medium, "Cap. 3 CNA", DeadlineCertainty.Confirmed),
        };

        modelBuilder.Entity<DeadlineRule>().HasData(rules.Select(r => new DeadlineRule
        {
            Id = DeterministicGuid($"deadline:{r.Item1}"),
            Code = r.Item1,
            Name = r.Item2,
            BaseEvent = r.Item3,
            OffsetHours = r.Item4,
            AtRiskWindowHours = r.Item5,
            Direction = r.Item6,
            Country = r.Item7,
            BLType = r.Item8,
            Severity = r.Item9,
            Source = r.Item10,
            Certainty = r.Item11,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        }));
    }

    /// <summary>GUID determinista y estable (no aleatorio) para datos semilla, derivado del código.</summary>
    private static Guid DeterministicGuid(string seed)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        return new Guid(bytes);
    }

    private static void SeedRbac(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Roles del sistema
        var roles = new (string Code, string Name)[]
        {
            (RoleCodes.Administrador, "Administrador"),
            (RoleCodes.Coordinador, "Coordinador"),
            (RoleCodes.Supervisor, "Supervisor"),
            (RoleCodes.ExternalApi, "External API"),
            (RoleCodes.Client, "Cliente"),
            (RoleCodes.CustomsAgent, "Agente de Aduana"),
            (RoleCodes.AdminBA, "Administrador BA"),
            (RoleCodes.SuperAdmin, "Super Administrador"),
            (RoleCodes.OrgAdmin, "Administrador de organización"),
            (RoleCodes.OrgOperator, "Operador de organización"),
            (RoleCodes.OrgViewer, "Consulta de organización"),
        };

        modelBuilder.Entity<Role>().HasData(roles.Select(r => new Role
        {
            Id = DeterministicGuid($"role:{r.Code}"),
            Code = r.Code,
            Name = r.Name,
            IsSystem = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        }));

        // Catálogo de permisos de la consola operativa
        var permissions = new[]
        {
            "users.manage", "roles.manage", "maintainers.manage",
            "config.global.manage", "config.client.manage",
            "bl.upload", "bl.view",
            "customs.transmit", "customs.retry", "customs.view",
            "deadlines.view", "audit.view", "reports.view", "notifications.view",
            // Fase 1 Ola A: perfiles de organización (M1-02) e internos (M8-04, M8-06, M1-11).
            AccessPermissions.ManageOrganizationUsers, AccessPermissions.ApproveJoinRequests,
            AccessPermissions.OperateShipments, AccessPermissions.ViewAllShipments,
            AccessPermissions.ReviewOrganizations, AccessPermissions.CheckOrganizationsAr,
            AccessPermissions.ManageAccessMatrix,
            // Fase 1 Ola B: accesos a terceros (M1-12 a M1-24).
            AccessPermissions.ManageThirdPartyAccess,
            // Fase 1 Ola D: Finanzas (M5-02, NF-03, NF-04) y bloqueo de pagos por horario (M8-07).
            PaymentPermissions.Finance, PaymentPermissions.ManageBlockWindows,
        };

        modelBuilder.Entity<Permission>().HasData(permissions.Select(p => new Permission
        {
            Id = DeterministicGuid($"perm:{p}"),
            Code = p,
            Description = p
        }));

        // Asignaciones base por rol (Administrador/SuperAdmin se resuelven como comodín en runtime).
        var assignments = new (string Role, string Perm)[]
        {
            (RoleCodes.Coordinador, "bl.upload"), (RoleCodes.Coordinador, "bl.view"),
            (RoleCodes.Coordinador, "customs.view"), (RoleCodes.Coordinador, "deadlines.view"),
            (RoleCodes.Coordinador, "reports.view"), (RoleCodes.Coordinador, "notifications.view"),
            (RoleCodes.Supervisor, "bl.view"), (RoleCodes.Supervisor, "customs.transmit"),
            (RoleCodes.Supervisor, "customs.retry"), (RoleCodes.Supervisor, "customs.view"),
            (RoleCodes.Supervisor, "deadlines.view"), (RoleCodes.Supervisor, "audit.view"),
            (RoleCodes.Supervisor, "reports.view"), (RoleCodes.Supervisor, "notifications.view"),
            (RoleCodes.ExternalApi, "bl.upload"), (RoleCodes.ExternalApi, "customs.transmit"),
            (RoleCodes.OrgAdmin, AccessPermissions.ManageOrganizationUsers),
            (RoleCodes.OrgAdmin, AccessPermissions.ApproveJoinRequests),
            (RoleCodes.OrgAdmin, AccessPermissions.OperateShipments),
            (RoleCodes.OrgAdmin, AccessPermissions.ManageThirdPartyAccess),
            (RoleCodes.OrgOperator, AccessPermissions.OperateShipments),
        };

        modelBuilder.Entity<RolePermission>().HasData(assignments.Select(a => new RolePermission
        {
            Id = DeterministicGuid($"rp:{a.Role}:{a.Perm}"),
            RoleId = DeterministicGuid($"role:{a.Role}"),
            PermissionId = DeterministicGuid($"perm:{a.Perm}")
        }));
    }

    /// <summary>
    /// Matriz base de M1-11 como datos administrables (no hardcode): una acción por fila del documento
    /// y una regla por rol, más las excepciones de Freight Forwarder.
    /// </summary>
    private static void SeedAccessMatrix(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<ShipmentAction>().HasData(AccessMatrixBaseline.Actions.Select((a, index) => new ShipmentAction
        {
            Id = DeterministicGuid($"shipment-action:{a.Code}"),
            Code = a.Code,
            Name = a.Name,
            Category = a.Category,
            Kind = a.Kind,
            Scope = a.Scope,
            DisplayOrder = (index + 1) * 10,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        }));

        var rules = AccessMatrixBaseline.Actions.SelectMany(a =>
            ShipmentRoleCodes.MatrixColumns
                .Select((role, i) => (a.Code, Role: role, OrganizationType: (string?)null, Level: a.Levels[i]))
                .Concat(a.Overrides.Select(o => (a.Code, o.Role, OrganizationType: (string?)o.OrganizationType, o.Level))));

        modelBuilder.Entity<ShipmentAccessRule>().HasData(rules.Select(r => new ShipmentAccessRule
        {
            Id = DeterministicGuid($"shipment-rule:{r.Code}:{r.Role}:{r.OrganizationType ?? "*"}"),
            ShipmentActionId = DeterministicGuid($"shipment-action:{r.Code}"),
            Role = r.Role,
            OrganizationType = r.OrganizationType,
            Level = r.Level,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        }));
    }

    /// <summary>
    /// Datos de demostración de Fase 1 Ola A: perfiles de organización, una solicitud de vinculación
    /// pendiente, una organización en validación (M8-04), roles por embarque y BL de exportación.
    /// </summary>
    private static void SeedOrganizationAccessDemo(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // BCrypt hash of "Admin123!" with work factor 12 (reused for all demo users)
        const string demoPasswordHash = "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca";

        // ── Organizaciones ────────────────────────────────────────────
        modelBuilder.Entity<Client>().HasData(
            // Cliente titular de un BL de exportación donde Importadora Demo solo es Shipper.
            new Client
            {
                Id = SeedDataIds.PacificTradingClient,
                Name = "Pacific Trading Co.",
                TaxId = "77.888.999-0",
                TaxIdType = "RUT",
                Country = CountryCodes.Chile,
                Email = "contacto@pacifictrading.cl",
                ClientType = "Client",
                OrganizationType = OrganizationTypes.Customer,
                RegistrationStatus = OrganizationStatus.Approved,
                MatchCode = "MC100040",
                OperatingCountries = "CL",
                ApprovedAt = now,
                IsActive = true,
                IsEmailConfirmed = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Registro nuevo pendiente de validación interna (bandeja de M8-04).
            new Client
            {
                Id = SeedDataIds.PendingOrgClient,
                Name = "Logística Andina SpA",
                TaxId = "76.555.111-2",
                TaxIdType = "RUT",
                Country = CountryCodes.Chile,
                Email = "registro@logisticaandina.cl",
                Phone = "+56 2 2999 1234",
                ClientType = "Client",
                OrganizationType = OrganizationTypes.FreightForwarder,
                RegistrationStatus = OrganizationStatus.PendingValidation,
                OperatingCountries = "CL",
                IsActive = true,
                IsEmailConfirmed = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ── Usuarios y perfiles ───────────────────────────────────────
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = SeedDataIds.DemoViewerUserCL,
                Username = "consulta@importadorademo.cl",
                Email = "consulta@importadorademo.cl",
                PasswordHash = demoPasswordHash,
                UserType = "Client",
                Country = CountryCodes.Chile,
                FirstName = "Carla",
                LastName = "Consulta",
                IsActive = true,
                MembershipStatus = MembershipStatus.Active,
                ClientId = SeedDataIds.DemoClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new User
            {
                Id = SeedDataIds.DemoJoinRequestUserCL,
                Username = "solicitud@importadorademo.cl",
                Email = "solicitud@importadorademo.cl",
                PasswordHash = demoPasswordHash,
                UserType = "Client",
                Country = CountryCodes.Chile,
                FirstName = "Sergio",
                LastName = "Solicitante",
                IsActive = true,
                MembershipStatus = MembershipStatus.Pending,
                ClientId = SeedDataIds.DemoClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new User
            {
                Id = SeedDataIds.PendingOrgUser,
                Username = "admin@logisticaandina.cl",
                Email = "admin@logisticaandina.cl",
                PasswordHash = demoPasswordHash,
                UserType = "Client",
                Country = CountryCodes.Chile,
                FirstName = "Andrea",
                LastName = "Andina",
                IsActive = true,
                MembershipStatus = MembershipStatus.Active,
                ClientId = SeedDataIds.PendingOrgClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        var profiles = new (Guid UserId, string Profile)[]
        {
            (SeedDataIds.DemoUserCL, RoleCodes.OrgAdmin),
            (SeedDataIds.DemoUserBO, RoleCodes.OrgAdmin),
            (SeedDataIds.AgentUserCL, RoleCodes.OrgAdmin),
            (SeedDataIds.DemoViewerUserCL, RoleCodes.OrgViewer),
            (SeedDataIds.PendingOrgUser, RoleCodes.OrgAdmin),
        };

        modelBuilder.Entity<UserRole>().HasData(profiles.Select(p => new UserRole
        {
            Id = DeterministicGuid($"user-profile:{p.UserId}"),
            UserId = p.UserId,
            RoleName = p.Profile,
            RoleId = DeterministicGuid($"role:{p.Profile}")
        }));

        // ── Embarques de exportación ──────────────────────────────────
        modelBuilder.Entity<BillOfLading>().HasData(
            // CL exportación: Importadora Demo es titular (Customer) y Shipper.
            new BillOfLading
            {
                Id = SeedDataIds.BL06,
                BLNumber = "HLCUSAI260300610",
                BookingNumber = "HLCUBKG2603061",
                ShipmentType = "Export",
                Vessel = "Valparaiso Express",
                Voyage = "2610S",
                PortOfLoading = "San Antonio (CLSAI)",
                PortOfDischarge = "Rotterdam (NLRTM)",
                PlaceOfDelivery = "Rotterdam, Netherlands",
                ETD = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                ETA = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc),
                Shipper = "Importadora Demo SpA",
                Consignee = "Fruit Import BV",
                FreightAmount = 3900m,
                FreightCurrency = "USD",
                Status = "Booked",
                Country = CountryCodes.Chile,
                ClientId = SeedDataIds.DemoClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // CL exportación: Pacific Trading es titular; Importadora Demo solo es Shipper (flete X (o)).
            new BillOfLading
            {
                Id = SeedDataIds.BL07,
                BLNumber = "HLCUVAP260300720",
                BookingNumber = "HLCUBKG2603072",
                ShipmentType = "Export",
                Vessel = "Santos Express",
                Voyage = "2611N",
                PortOfLoading = "Valparaiso (CLVAP)",
                PortOfDischarge = "Shanghai (CNSHA)",
                PlaceOfDelivery = "Shanghai, China",
                ETD = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
                ETA = new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                Shipper = "Importadora Demo SpA",
                Consignee = "Shanghai Wine Trading Ltd",
                FreightAmount = 4100m,
                FreightCurrency = "USD",
                Status = "Loaded",
                Country = CountryCodes.Chile,
                ClientId = SeedDataIds.PacificTradingClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // BO exportación: Comercial Altiplano es titular y Shipper.
            new BillOfLading
            {
                Id = SeedDataIds.BL08,
                BLNumber = "HLCUARI260300830",
                BookingNumber = "HLCUBKG2603083",
                ShipmentType = "Export",
                Vessel = "Antofagasta Express",
                Voyage = "2612S",
                PortOfLoading = "Arica (CLARI)",
                PortOfDischarge = "Callao (PECLL)",
                PlaceOfDelivery = "Lima, Peru",
                ETD = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                ETA = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc),
                Shipper = "Comercial Altiplano SRL",
                Consignee = "Andes Foods SAC",
                FreightAmount = 1450m,
                FreightCurrency = "USD",
                Status = "Booked",
                Country = CountryCodes.Bolivia,
                ClientId = SeedDataIds.DemoClientBO,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        modelBuilder.Entity<BLContainer>().HasData(
            new BLContainer { Id = SeedDataIds.Container08, ContainerNumber = "HLXU2023001", ContainerType = "40RF", SealNumber = "SL-020301", Weight = 26800m, Status = "GateIn", BillOfLadingId = SeedDataIds.BL06, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container09, ContainerNumber = "HLXU2023002", ContainerType = "20DV", SealNumber = "SL-020302", Weight = 17400m, Status = "OnBoard", BillOfLadingId = SeedDataIds.BL07, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container10, ContainerNumber = "HLXU2023003", ContainerType = "20DV", SealNumber = "SL-020303", Weight = 16900m, Status = "Empty", BillOfLadingId = SeedDataIds.BL08, CreatedAt = now, CreatedBy = "SYSTEM" });

        modelBuilder.Entity<LocalCharge>().HasData(
            new LocalCharge { Id = SeedDataIds.LocalCharge09, ChargeType = ChargeConceptCodes.GateIn, Description = "Gate In - 40RF (San Antonio)", Amount = 95000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 18050m, TotalAmount = 113050m, BillOfLadingId = SeedDataIds.BL06, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge10, ChargeType = "BL_FEE", Description = "BL Documentation Fee (export)", Amount = 45000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 8550m, TotalAmount = 53550m, BillOfLadingId = SeedDataIds.BL06, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge11, ChargeType = "THC", Description = "Terminal Handling Charge - 20DV (export)", Amount = 150000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 28500m, TotalAmount = 178500m, BillOfLadingId = SeedDataIds.BL07, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge12, ChargeType = ChargeConceptCodes.GateIn, Description = "Gate In - 20DV (Arica)", Amount = 820m, Currency = "BOB", Status = "Pending", IsTaxable = true, TaxRate = 13m, TaxAmount = 106.60m, TotalAmount = 926.60m, BillOfLadingId = SeedDataIds.BL08, CreatedAt = now, CreatedBy = "SYSTEM" });

        // ODS de exportación visible en el detalle del embarque (CL-EXP-13).
        modelBuilder.Entity<ServiceOrder>().HasData(new ServiceOrder
        {
            Id = SeedDataIds.ServiceOrder04,
            OrderNumber = "SO-2026-00004",
            OrderType = "GateIn",
            Status = "Pending",
            Description = "Recepción de contenedor reefer HLXU2023001 para exportación",
            Country = CountryCodes.Chile,
            RequestedAt = new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc),
            BillOfLadingId = SeedDataIds.BL06,
            ClientId = SeedDataIds.DemoClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // ── Roles por embarque (además del titular, que es Customer) ──
        var shipmentRoles = new (Guid BlId, Guid ClientId, string Role)[]
        {
            (SeedDataIds.BL01, SeedDataIds.DemoClientCL, ShipmentRoleCodes.Consignee),
            (SeedDataIds.BL02, SeedDataIds.DemoClientCL, ShipmentRoleCodes.Consignee),
            (SeedDataIds.BL04, SeedDataIds.DemoClientBO, ShipmentRoleCodes.Consignee),
            (SeedDataIds.BL05, SeedDataIds.DemoClientBO, ShipmentRoleCodes.Consignee),
            (SeedDataIds.BL06, SeedDataIds.DemoClientCL, ShipmentRoleCodes.Shipper),
            (SeedDataIds.BL07, SeedDataIds.DemoClientCL, ShipmentRoleCodes.Shipper),
            (SeedDataIds.BL08, SeedDataIds.DemoClientBO, ShipmentRoleCodes.Shipper),
        };

        modelBuilder.Entity<ShipmentRole>().HasData(shipmentRoles.Select(r => new ShipmentRole
        {
            Id = DeterministicGuid($"shipment-role:{r.BlId}:{r.ClientId}:{r.Role}"),
            BillOfLadingId = r.BlId,
            ClientId = r.ClientId,
            Role = r.Role,
            Source = ShipmentRoleSources.Seed,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        }));
    }

    /// <summary>
    /// Datos de demostración de Fase 1 Ola B (M1-12 a M1-23): Importadora Demo otorga a la agencia de
    /// aduanas un acceso con permisos limitados sobre un BL y un mandato con términos aceptados sobre
    /// otro, la tiene como tercero por defecto, y Pacific Trading activa el acceso abierto por BL.
    /// </summary>
    private static void SeedThirdPartyAccessDemo(ModelBuilder modelBuilder)
    {
        var created = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var validTo = new DateTime(2027, 3, 31, 23, 59, 0, DateTimeKind.Utc);

        // Techo: lo que Importadora Demo (Customer y Consignee de BL01 y BL02) posee sobre esos BL.
        var customer = Array.IndexOf(ShipmentRoleCodes.MatrixColumns, ShipmentRoleCodes.Customer);
        var consignee = Array.IndexOf(ShipmentRoleCodes.MatrixColumns, ShipmentRoleCodes.Consignee);
        var ceiling = ActionCodeList.Format(AccessMatrixBaseline.Actions
            .Where(a => a.Scope == ShipmentActionScopes.Shipment
                && (a.Levels[customer] == AccessLevels.Allowed || a.Levels[consignee] == AccessLevels.Allowed))
            .Select(a => a.Code));

        modelBuilder.Entity<AccessGrant>().HasData(
            // Acceso individual con permisos limitados: consulta, recargos mandatorios y demurrage (X (o) otorgado).
            new AccessGrant
            {
                Id = SeedDataIds.DemoAccessGrant01,
                GrantorClientId = SeedDataIds.DemoClientCL,
                GrantorRole = ShipmentRoleCodes.Customer,
                GranteeClientId = SeedDataIds.AgentClientCL,
                BillOfLadingId = SeedDataIds.BL01,
                BookingNumber = "HLCUBKG2501001",
                GrantType = AccessGrantTypes.Individual,
                ActionCodes = ActionCodeList.Format(
                [
                    ShipmentActionCodes.ViewShipment, ShipmentActionCodes.ViewReleaseRequirements,
                    ShipmentActionCodes.ViewTracking, ShipmentActionCodes.ViewBlIssuance,
                    ShipmentActionCodes.PayMandatoryLocalCharges, ShipmentActionCodes.PayImportDemurrage
                ]),
                CeilingActionCodes = ceiling,
                ValidityType = AccessValidityTypes.UntilDate,
                ValidFrom = created,
                ValidTo = validTo,
                Status = AccessGrantStatus.Active,
                GrantedByUserId = SeedDataIds.DemoUserCL,
                CreatedAt = created,
                CreatedBy = "SYSTEM"
            },
            // Mandato digital (M1-03): alcance de pago de flete y recargos, vigencia definida, términos aceptados.
            new AccessGrant
            {
                Id = SeedDataIds.DemoAccessGrant02,
                GrantorClientId = SeedDataIds.DemoClientCL,
                GrantorRole = ShipmentRoleCodes.Customer,
                GranteeClientId = SeedDataIds.AgentClientCL,
                BillOfLadingId = SeedDataIds.BL02,
                BookingNumber = "HLCUBKG2502004",
                GrantType = AccessGrantTypes.Individual,
                ActionCodes = ActionCodeList.Format(
                [
                    ShipmentActionCodes.ViewShipment, ShipmentActionCodes.PayFreight,
                    ShipmentActionCodes.PayMandatoryLocalCharges
                ]),
                CeilingActionCodes = ceiling,
                ValidityType = AccessValidityTypes.UntilDate,
                ValidFrom = created,
                ValidTo = validTo,
                Status = AccessGrantStatus.Active,
                GrantedByUserId = SeedDataIds.DemoUserCL,
                IsMandate = true,
                TermsVersion = MandateTerms.CurrentVersion,
                TermsAcceptedAt = created,
                TermsAcceptedByUserId = SeedDataIds.DemoUserCL,
                CreatedAt = created,
                CreatedBy = "SYSTEM"
            });

        // Tercero por defecto (M1-13): nivel base de M1-11, 180 días por BL nuevo.
        modelBuilder.Entity<DefaultGrantee>().HasData(new DefaultGrantee
        {
            Id = SeedDataIds.DemoDefaultGrantee01,
            GrantorClientId = SeedDataIds.DemoClientCL,
            GranteeClientId = SeedDataIds.AgentClientCL,
            DurationDays = 180,
            IsActive = true,
            CreatedAt = created,
            CreatedBy = "SYSTEM"
        });

        // Acceso abierto por número de BL (M1-17) de Pacific Trading, titular de BL07.
        modelBuilder.Entity<OpenAccessSetting>().HasData(new OpenAccessSetting
        {
            Id = SeedDataIds.DemoOpenAccessSetting01,
            ClientId = SeedDataIds.PacificTradingClient,
            IsEnabled = true,
            ActionCodes = ActionCodeList.Format(
            [
                ShipmentActionCodes.ViewShipment, ShipmentActionCodes.ViewTracking, ShipmentActionCodes.ViewBlIssuance,
                ShipmentActionCodes.ViewReleaseRequirements, ShipmentActionCodes.PayMandatoryLocalCharges
            ]),
            ChangedAt = created,
            CreatedAt = created,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<AccessAuditEntry>().HasData(
            new AccessAuditEntry
            {
                Id = SeedDataIds.DemoAccessAudit01,
                OccurredAt = created,
                EventType = AccessAuditEvents.GrantCreated,
                BillOfLadingId = SeedDataIds.BL01,
                BlNumber = "HLCUVAL250100123",
                BookingNumber = "HLCUBKG2501001",
                AccessGrantId = SeedDataIds.DemoAccessGrant01,
                GrantorClientId = SeedDataIds.DemoClientCL,
                GranteeClientId = SeedDataIds.AgentClientCL,
                ActorUserId = SeedDataIds.DemoUserCL,
                ActorEmail = "demo@importadorademo.cl",
                ActorClientId = SeedDataIds.DemoClientCL,
                Details = """{"grantType":"Individual","source":"seed"}"""
            },
            new AccessAuditEntry
            {
                Id = SeedDataIds.DemoAccessAudit02,
                OccurredAt = created,
                EventType = AccessAuditEvents.GrantCreated,
                BillOfLadingId = SeedDataIds.BL02,
                BlNumber = "HLCUVAL250200456",
                BookingNumber = "HLCUBKG2502004",
                AccessGrantId = SeedDataIds.DemoAccessGrant02,
                GrantorClientId = SeedDataIds.DemoClientCL,
                GranteeClientId = SeedDataIds.AgentClientCL,
                ActorUserId = SeedDataIds.DemoUserCL,
                ActorEmail = "demo@importadorademo.cl",
                ActorClientId = SeedDataIds.DemoClientCL,
                Details = """{"grantType":"Individual","isMandate":true,"source":"seed"}"""
            },
            new AccessAuditEntry
            {
                Id = SeedDataIds.DemoAccessAudit03,
                OccurredAt = created,
                EventType = AccessAuditEvents.MandateTermsAccepted,
                BillOfLadingId = SeedDataIds.BL02,
                BlNumber = "HLCUVAL250200456",
                BookingNumber = "HLCUBKG2502004",
                AccessGrantId = SeedDataIds.DemoAccessGrant02,
                GrantorClientId = SeedDataIds.DemoClientCL,
                GranteeClientId = SeedDataIds.AgentClientCL,
                ActorUserId = SeedDataIds.DemoUserCL,
                ActorEmail = "demo@importadorademo.cl",
                ActorClientId = SeedDataIds.DemoClientCL,
                Details = $$"""{"termsVersion":"{{MandateTerms.CurrentVersion}}"}"""
            },
            new AccessAuditEntry
            {
                Id = SeedDataIds.DemoAccessAudit04,
                OccurredAt = created,
                EventType = AccessAuditEvents.DefaultGranteeAdded,
                GrantorClientId = SeedDataIds.DemoClientCL,
                GranteeClientId = SeedDataIds.AgentClientCL,
                ActorUserId = SeedDataIds.DemoUserCL,
                ActorEmail = "demo@importadorademo.cl",
                ActorClientId = SeedDataIds.DemoClientCL,
                Details = """{"durationDays":180,"source":"seed"}"""
            },
            new AccessAuditEntry
            {
                Id = SeedDataIds.DemoAccessAudit05,
                OccurredAt = created,
                EventType = AccessAuditEvents.OpenAccessEnabled,
                GrantorClientId = SeedDataIds.PacificTradingClient,
                ActorEmail = "seed",
                Details = """{"isEnabled":true,"source":"seed"}"""
            });
    }


    private static void SeedCurrencies(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Currency>().HasData(
            new Currency
            {
                Id = SeedDataIds.CurrencyCLP,
                Code = "CLP",
                Name = "Peso Chileno",
                Symbol = "$",
                ExchangeRateToUSD = 950m,
                LastUpdated = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new Currency
            {
                Id = SeedDataIds.CurrencyBOB,
                Code = "BOB",
                Name = "Boliviano",
                Symbol = "Bs",
                ExchangeRateToUSD = 6.91m,
                LastUpdated = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new Currency
            {
                Id = SeedDataIds.CurrencyUSD,
                Code = "USD",
                Name = "US Dollar",
                Symbol = "$",
                ExchangeRateToUSD = 1m,
                LastUpdated = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Ola D: los fletes pueden pagarse en EUR (ejemplo de M5-04).
            new Currency
            {
                Id = SeedDataIds.CurrencyEUR,
                Code = "EUR",
                Name = "Euro",
                Symbol = "€",
                ExchangeRateToUSD = 0.92m,
                LastUpdated = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });
    }

    private static void SeedTaxConfigurations(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<TaxConfiguration>().HasData(
            new TaxConfiguration
            {
                Id = SeedDataIds.TaxIvaCL,
                Country = CountryCodes.Chile,
                ServiceType = "General",
                TaxName = "IVA",
                TaxRate = 19m,
                IsActive = true,
                EffectiveFrom = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new TaxConfiguration
            {
                Id = SeedDataIds.TaxIvaBO,
                Country = CountryCodes.Bolivia,
                ServiceType = "General",
                TaxName = "IVA",
                TaxRate = 13m,
                IsActive = true,
                EffectiveFrom = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });
    }

    private static void SeedAdminClientAndUser(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // BCrypt hash of "Admin123!" with work factor 12
        const string adminPasswordHash = "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca";

        modelBuilder.Entity<Client>().HasData(new Client
        {
            Id = SeedDataIds.AdminClient,
            Name = "Hapag-Lloyd Administrador",
            TaxId = "99999999-K",
            TaxIdType = "RUT",
            Country = CountryCodes.Chile,
            Email = "admin@hapag-lloyd.cl",
            Phone = "+56 2 2630 1700",
            ClientType = "Internal",
            OrganizationType = OrganizationTypes.Internal,
            RegistrationStatus = OrganizationStatus.Approved,
            OperatingCountries = "CL,BO",
            ApprovedAt = now,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedDataIds.AdminUser,
            Username = "admin@hapag-lloyd.cl",
            Email = "admin@hapag-lloyd.cl",
            PasswordHash = adminPasswordHash,
            UserType = "Admin",
            Country = CountryCodes.Chile,
            IsActive = true,
            ClientId = SeedDataIds.AdminClient,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = SeedDataIds.AdminUserRole,
            UserId = SeedDataIds.AdminUser,
            RoleName = "Admin",
            RoleId = DeterministicGuid($"role:{RoleCodes.Administrador}")
        });
    }

    private static void SeedFAQs(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<FAQ>().HasData(
            // Chile FAQs
            new FAQ
            {
                Id = SeedDataIds.FaqCL01,
                Question = "¿Cómo puedo consultar el estado de mi BL?",
                Answer = "Ingrese al módulo 'Bills of Lading', escriba su número de BL en el buscador y presione buscar. Verá el detalle completo incluyendo contenedores, cargos locales y demurrage.",
                Category = "SHIPPING",
                Country = CountryCodes.Chile,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqCL02,
                Question = "¿Qué métodos de pago están disponibles en Chile?",
                Answer = "En Chile puede pagar con Tarjeta de Crédito, Tarjeta de Débito, Transferencia Bancaria y WebPay. Todos los pagos electrónicos se procesan en tiempo real.",
                Category = "PAYMENTS",
                Country = CountryCodes.Chile,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqCL03,
                Question = "¿Cómo solicito un cambio de almacén?",
                Answer = "Vaya al módulo 'Cambio de Almacén', ingrese el número de BL, el contenedor, el almacén actual y el almacén destino. La solicitud será procesada y recibirá confirmación por correo.",
                Category = "SHIPPING",
                Country = CountryCodes.Chile,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqCL04,
                Question = "¿Cuál es la tasa de IVA aplicada en Chile?",
                Answer = "La tasa de IVA vigente en Chile es del 19%. Se aplica automáticamente sobre los cargos locales y servicios facturables.",
                Category = "PAYMENTS",
                Country = CountryCodes.Chile,
                SortOrder = 4,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqCL05,
                Question = "¿Cómo genero un recibo de pago?",
                Answer = "Una vez confirmado el pago, vaya al detalle del pago y presione 'Generar Recibo'. El recibo se genera automáticamente en formato PDF con todos los datos fiscales.",
                Category = "DOCUMENTATION",
                Country = CountryCodes.Chile,
                SortOrder = 5,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Bolivia FAQs
            new FAQ
            {
                Id = SeedDataIds.FaqBO01,
                Question = "¿Cómo puedo consultar el estado de mi BL en Bolivia?",
                Answer = "Ingrese al módulo 'Bills of Lading' y busque por número de BL. Verá el estado de su carga incluyendo el puerto de ingreso (Arica, Iquique o Antofagasta) y los cargos asociados.",
                Category = "SHIPPING",
                Country = CountryCodes.Bolivia,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqBO02,
                Question = "¿Qué métodos de pago están disponibles en Bolivia?",
                Answer = "En Bolivia puede pagar mediante Transferencia Bancaria, Efectivo y Cheque. Los pagos en efectivo deben realizarse en oficinas autorizadas.",
                Category = "PAYMENTS",
                Country = CountryCodes.Bolivia,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqBO03,
                Question = "¿Cuál es la tasa de IVA aplicada en Bolivia?",
                Answer = "La tasa de IVA vigente en Bolivia es del 13%. Se aplica automáticamente sobre los cargos locales y servicios facturables. Los montos se manejan en Bolivianos (BOB).",
                Category = "PAYMENTS",
                Country = CountryCodes.Bolivia,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqBO04,
                Question = "¿Qué es el NIT y por qué lo necesito?",
                Answer = "El NIT (Número de Identificación Tributaria) es el identificador fiscal en Bolivia. Es obligatorio para el registro en el portal y para la emisión de documentos fiscales.",
                Category = "GENERAL",
                Country = CountryCodes.Bolivia,
                SortOrder = 4,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqBO05,
                Question = "¿Cómo funciona el demurrage para carga en tránsito a Bolivia?",
                Answer = "El demurrage se calcula desde la fecha de descarga en el puerto chileno. Los días libres y tarifas diarias dependen del tipo de contenedor y acuerdos comerciales. Puede solicitar exenciones a través del módulo de Demurrage.",
                Category = "DEMURRAGE",
                Country = CountryCodes.Bolivia,
                SortOrder = 5,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // General FAQs (both countries)
            new FAQ
            {
                Id = SeedDataIds.FaqGen01,
                Question = "¿Cómo registro mi empresa en el portal?",
                Answer = "Haga clic en 'Registrarse', seleccione su país (Chile o Bolivia), ingrese los datos de su empresa (RUT/NIT, nombre, correo) y cree una contraseña. Recibirá un correo de confirmación.",
                Category = "GENERAL",
                Country = CountryCodes.Chile,
                SortOrder = 10,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqGen02,
                Question = "¿Olvidé mi contraseña, cómo la recupero?",
                Answer = "En la pantalla de login, haga clic en '¿Olvidó su contraseña?'. Ingrese su correo electrónico y recibirá un enlace para restablecer su contraseña.",
                Category = "GENERAL",
                Country = CountryCodes.Chile,
                SortOrder = 11,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqGen03,
                Question = "¿Cómo contacto a soporte técnico?",
                Answer = "Puede contactarnos al correo clservice@hapag-lloyd.com o llamar al +56 2 2630 1700 (Chile) / +591 2 211 0700 (Bolivia) en horario de oficina de lunes a viernes.",
                Category = "GENERAL",
                Country = CountryCodes.Chile,
                SortOrder = 12,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });
    }

    private static void SeedDemoData(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // BCrypt hash of "Admin123!" with work factor 12 (reused for all demo users)
        const string demoPasswordHash = "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca";

        // ──────────────────────────────────────────────────────────────
        // CLIENTS & USERS
        // ──────────────────────────────────────────────────────────────

        // Demo client — Chilean importer
        modelBuilder.Entity<Client>().HasData(new Client
        {
            Id = SeedDataIds.DemoClientCL,
            Name = "Importadora Demo SpA",
            TaxId = "76.123.456-7",
            TaxIdType = "RUT",
            Country = CountryCodes.Chile,
            Email = "demo@importadorademo.cl",
            Phone = "+56 2 2345 6789",
            Address = "Av. Providencia 1234, Of. 501",
            City = "Santiago",
            ClientType = "Client",
            OrganizationType = OrganizationTypes.Customer,
            RegistrationStatus = OrganizationStatus.Approved,
            MatchCode = "MC100010",
            OperatingCountries = "CL,BO",
            ApprovedAt = now,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedDataIds.DemoUserCL,
            Username = "demo@importadorademo.cl",
            Email = "demo@importadorademo.cl",
            PasswordHash = demoPasswordHash,
            UserType = "Client",
            Country = CountryCodes.Chile,
            IsActive = true,
            ClientId = SeedDataIds.DemoClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = SeedDataIds.DemoUserRoleCL,
            UserId = SeedDataIds.DemoUserCL,
            RoleName = "User",
            RoleId = DeterministicGuid($"role:{RoleCodes.Client}")
        });

        // Demo client — Bolivian importer
        modelBuilder.Entity<Client>().HasData(new Client
        {
            Id = SeedDataIds.DemoClientBO,
            Name = "Comercial Altiplano SRL",
            TaxId = "1023456017",
            TaxIdType = "NIT",
            Country = CountryCodes.Bolivia,
            Email = "demo@altiplano.bo",
            Phone = "+591 2 211 5678",
            Address = "Calle Comercio 456",
            City = "La Paz",
            ClientType = "Client",
            OrganizationType = OrganizationTypes.Customer,
            RegistrationStatus = OrganizationStatus.Approved,
            MatchCode = "MC100020",
            OperatingCountries = "BO",
            ApprovedAt = now,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedDataIds.DemoUserBO,
            Username = "demo@altiplano.bo",
            Email = "demo@altiplano.bo",
            PasswordHash = demoPasswordHash,
            UserType = "Client",
            Country = CountryCodes.Bolivia,
            IsActive = true,
            ClientId = SeedDataIds.DemoClientBO,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = SeedDataIds.DemoUserRoleBO,
            UserId = SeedDataIds.DemoUserBO,
            RoleName = "User",
            RoleId = DeterministicGuid($"role:{RoleCodes.Client}")
        });

        // Agent client — Chilean freight agent
        modelBuilder.Entity<Client>().HasData(new Client
        {
            Id = SeedDataIds.AgentClientCL,
            Name = "Agencia Marítima del Pacífico Ltda",
            TaxId = "96.555.444-3",
            TaxIdType = "RUT",
            Country = CountryCodes.Chile,
            Email = "agente@maritimpacifico.cl",
            Phone = "+56 32 225 1000",
            Address = "Blanco 1199, Of. 301",
            City = "Valparaíso",
            ClientType = "Agent",
            AgentCode = "AGT-CL-001",
            OrganizationType = OrganizationTypes.CustomsAgency,
            RegistrationStatus = OrganizationStatus.Approved,
            MatchCode = "MC100030",
            OperatingCountries = "CL",
            ApprovedAt = now,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedDataIds.AgentUserCL,
            Username = "agente@maritimpacifico.cl",
            Email = "agente@maritimpacifico.cl",
            PasswordHash = demoPasswordHash,
            UserType = "Agent",
            Country = CountryCodes.Chile,
            IsActive = true,
            ClientId = SeedDataIds.AgentClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = SeedDataIds.AgentUserRoleCL,
            UserId = SeedDataIds.AgentUserCL,
            RoleName = "User",
            RoleId = DeterministicGuid($"role:{RoleCodes.CustomsAgent}")
        });

        // ──────────────────────────────────────────────────────────────
        // BILLS OF LADING (5 total: 3 CL, 2 BO)
        // ──────────────────────────────────────────────────────────────

        // BL 1 — Chile, Arrived, 2 containers
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL01,
            BLNumber = "HLCUVAL250100123",
            BookingNumber = "HLCUBKG2501001",
            ShipmentType = "Import",
            Vessel = "Hamburg Express",
            Voyage = "025E",
            PortOfLoading = "Shanghai (CNSHA)",
            PortOfDischarge = "San Antonio (CLSAI)",
            PlaceOfDelivery = "Santiago, Chile",
            ETD = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Importadora Demo SpA",
            Shipper = "Shanghai Electronics Co. Ltd",
            NotifyParty = "Agencia Marítima del Pacífico Ltda",
            FreightAmount = 3500m,
            FreightCurrency = "USD",
            Status = "Arrived",
            Country = CountryCodes.Chile,
            ClientId = SeedDataIds.DemoClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // BL 2 — Chile, InTransit, 2 containers
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL02,
            BLNumber = "HLCUVAL250200456",
            BookingNumber = "HLCUBKG2502004",
            ShipmentType = "Import",
            Vessel = "Berlin Express",
            Voyage = "031W",
            PortOfLoading = "Busan (KRPUS)",
            PortOfDischarge = "Valparaiso (CLVAP)",
            PlaceOfDelivery = "Valparaiso, Chile",
            ETD = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Importadora Demo SpA",
            Shipper = "Korea Auto Parts Inc.",
            FreightAmount = 5200m,
            FreightCurrency = "USD",
            // Ola D: flete pagado por la agencia bajo mandato (pago PAY-20260920-5E6F7A8B).
            FreightPaidAt = new DateTime(2026, 9, 20, 14, 5, 0, DateTimeKind.Utc),
            Status = "InTransit",
            Country = CountryCodes.Chile,
            ClientId = SeedDataIds.DemoClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // BL 3 — Chile, Delivered, 1 container (admin client)
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL03,
            BLNumber = "HLCUVAL250300789",
            BookingNumber = "HLCUBKG2503007",
            ShipmentType = "Import",
            Vessel = "Colombo Express",
            Voyage = "018E",
            PortOfLoading = "Rotterdam (NLRTM)",
            PortOfDischarge = "San Antonio (CLSAI)",
            PlaceOfDelivery = "Santiago, Chile",
            ETD = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Hapag-Lloyd Administrador",
            Shipper = "European Machinery GmbH",
            FreightAmount = 8750m,
            FreightCurrency = "USD",
            Status = "Delivered",
            Country = CountryCodes.Chile,
            ClientId = SeedDataIds.AdminClient,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // BL 4 — Bolivia, Arrived via Arica, 1 container
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL04,
            BLNumber = "HLCUARI260100045",
            BookingNumber = "HLCUBKG2601045",
            ShipmentType = "Import",
            Vessel = "Antofagasta Express",
            Voyage = "012E",
            PortOfLoading = "Ningbo (CNNGB)",
            PortOfDischarge = "Arica (CLARI)",
            PlaceOfDelivery = "La Paz, Bolivia",
            ETD = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Comercial Altiplano SRL",
            Shipper = "Ningbo Textiles Export Co.",
            FreightAmount = 2800m,
            FreightCurrency = "USD",
            Status = "Arrived",
            Country = CountryCodes.Bolivia,
            ClientId = SeedDataIds.DemoClientBO,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // BL 5 — Bolivia, InTransit via Iquique, 1 container
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL05,
            BLNumber = "HLCUIQQ260200078",
            BookingNumber = "HLCUBKG2602078",
            ShipmentType = "Import",
            Vessel = "Guayaquil Express",
            Voyage = "007W",
            PortOfLoading = "Mumbai (INBOM)",
            PortOfDischarge = "Iquique (CLIQQ)",
            PlaceOfDelivery = "Santa Cruz, Bolivia",
            ETD = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Comercial Altiplano SRL",
            Shipper = "Mumbai Spices & Commodities Pvt Ltd",
            FreightAmount = 1950m,
            FreightCurrency = "USD",
            Status = "InTransit",
            Country = CountryCodes.Bolivia,
            ClientId = SeedDataIds.DemoClientBO,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // ──────────────────────────────────────────────────────────────
        // CONTAINERS (7 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<BLContainer>().HasData(
            new BLContainer { Id = SeedDataIds.Container01, ContainerNumber = "HLXU1234567", ContainerType = "40HC", SealNumber = "SL-001234", Weight = 24500m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container02, ContainerNumber = "HLXU7654321", ContainerType = "20DV", SealNumber = "SL-005678", Weight = 18200m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container03, ContainerNumber = "HLXU9876543", ContainerType = "40HC", SealNumber = "SL-009012", Weight = 22100m, Status = "OnBoard", BillOfLadingId = SeedDataIds.BL02, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container04, ContainerNumber = "HLXU1112233", ContainerType = "40RF", SealNumber = "SL-003456", Weight = 19800m, Status = "OnBoard", BillOfLadingId = SeedDataIds.BL02, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container05, ContainerNumber = "HLXU4455667", ContainerType = "40OT", SealNumber = "SL-007890", Weight = 31500m, Status = "Delivered", BillOfLadingId = SeedDataIds.BL03, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container06, ContainerNumber = "HLXU8899001", ContainerType = "20DV", SealNumber = "SL-011234", Weight = 15600m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container07, ContainerNumber = "HLXU5566778", ContainerType = "40HC", SealNumber = "SL-015678", Weight = 21300m, Status = "OnBoard", BillOfLadingId = SeedDataIds.BL05, CreatedAt = now, CreatedBy = "SYSTEM" });

        // ──────────────────────────────────────────────────────────────
        // LOCAL CHARGES (8 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<LocalCharge>().HasData(
            // BL 1 charges (CL, Pending)
            new LocalCharge { Id = SeedDataIds.LocalCharge01, ChargeType = "THC", Description = "Terminal Handling Charge - 40HC", Amount = 185000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 35150m, TotalAmount = 220150m, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge02, ChargeType = "BL_FEE", Description = "BL Documentation Fee", Amount = 45000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 8550m, TotalAmount = 53550m, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge03, ChargeType = "ISPS", Description = "ISPS Security Surcharge", Amount = 25000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 4750m, TotalAmount = 29750m, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            // BL 2 charges (CL, Pending)
            new LocalCharge { Id = SeedDataIds.LocalCharge04, ChargeType = "THC", Description = "Terminal Handling Charge - 40HC", Amount = 185000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 35150m, TotalAmount = 220150m, BillOfLadingId = SeedDataIds.BL02, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge05, ChargeType = "THC_RF", Description = "Terminal Handling Charge - 40RF Reefer", Amount = 295000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 56050m, TotalAmount = 351050m, BillOfLadingId = SeedDataIds.BL02, CreatedAt = now, CreatedBy = "SYSTEM" },
            // BL 3 charges (CL, Paid)
            new LocalCharge { Id = SeedDataIds.LocalCharge06, ChargeType = "THC", Description = "Terminal Handling Charge - 40OT", Amount = 210000m, Currency = "CLP", Status = "Paid", IsTaxable = true, TaxRate = 19m, TaxAmount = 39900m, TotalAmount = 249900m, BillOfLadingId = SeedDataIds.BL03, CreatedAt = now, CreatedBy = "SYSTEM" },
            // BL 4 charges (BO, Pending — BOB currency, 13% IVA)
            new LocalCharge { Id = SeedDataIds.LocalCharge07, ChargeType = "THC", Description = "Terminal Handling Charge - 20DV (Arica)", Amount = 1280m, Currency = "BOB", Status = "Pending", IsTaxable = true, TaxRate = 13m, TaxAmount = 166.40m, TotalAmount = 1446.40m, BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge08, ChargeType = "TRANSIT_FEE", Description = "Bolivia Transit Documentation Fee", Amount = 690m, Currency = "BOB", Status = "Pending", IsTaxable = true, TaxRate = 13m, TaxAmount = 89.70m, TotalAmount = 779.70m, BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" });

        // ──────────────────────────────────────────────────────────────
        // DEMURRAGE CHARGES (3 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<DemurrageCharge>().HasData(
            new DemurrageCharge { Id = SeedDataIds.Demurrage01, ContainerNumber = "HLXU1234567", FreeDays = 7, DemurrageDays = 5, DailyRate = 45000m, TotalAmount = 225000m, Currency = "CLP", StartDate = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), Status = "Pending", IsExempt = false, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new DemurrageCharge { Id = SeedDataIds.Demurrage02, ContainerNumber = "HLXU7654321", FreeDays = 7, DemurrageDays = 3, DailyRate = 35000m, TotalAmount = 105000m, Currency = "CLP", StartDate = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), Status = "Pending", IsExempt = false, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new DemurrageCharge { Id = SeedDataIds.Demurrage03, ContainerNumber = "HLXU8899001", FreeDays = 10, DemurrageDays = 8, DailyRate = 310m, TotalAmount = 2480m, Currency = "BOB", StartDate = new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), Status = "Pending", IsExempt = false, BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" });

        // ──────────────────────────────────────────────────────────────
        // PAYMENTS (8 total: all types, all statuses, all methods)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<Payment>().HasData(
            // Payment 1 — BL3 local charges, confirmed (WebPay) — Admin client
            new Payment
            {
                Id = SeedDataIds.Payment01,
                PaymentNumber = "PAY-2026-00001",
                PaymentType = "LocalCharges",
                PaymentMethod = "WebPay",
                Amount = 210000m,
                TaxAmount = 39900m,
                TotalAmount = 249900m,
                Currency = "CLP",
                Status = "Confirmed",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 2, 20, 14, 30, 0, DateTimeKind.Utc),
                ConfirmedAt = new DateTime(2026, 2, 20, 14, 31, 0, DateTimeKind.Utc),
                ConfirmedBy = "WEBPAY_AUTO",
                ExternalReference = "WBP-TXN-20260220-001",
                ReceiptNumber = "REC-2026-00001",
                ClientId = SeedDataIds.AdminClient,
                BillOfLadingId = SeedDataIds.BL03,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 2 — BL1 local charges, pending (bank transfer) — Demo CL
            new Payment
            {
                Id = SeedDataIds.Payment02,
                PaymentNumber = "PAY-2026-00002",
                PaymentType = "LocalCharges",
                PaymentMethod = "BankTransfer",
                Amount = 230000m,
                TaxAmount = 43700m,
                TotalAmount = 273700m,
                Currency = "CLP",
                Status = "Pending",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc),
                DepositProofUrl = "/uploads/deposit-proof-002.pdf",
                ClientId = SeedDataIds.DemoClientCL,
                BillOfLadingId = SeedDataIds.BL01,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 3 — BL4 Bolivia charges, pending (Khipu/QR) — Demo BO
            new Payment
            {
                Id = SeedDataIds.Payment03,
                PaymentNumber = "PAY-2026-00003",
                PaymentType = "LocalCharges",
                PaymentMethod = "Khipu",
                Amount = 1970m,
                TaxAmount = 256.10m,
                TotalAmount = 2226.10m,
                Currency = "BOB",
                ExchangeRate = 6.91m,
                Status = "Pending",
                Country = CountryCodes.Bolivia,
                PaymentDate = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc),
                DepositProofUrl = "/uploads/deposit-proof-003.pdf",
                ClientId = SeedDataIds.DemoClientBO,
                BillOfLadingId = SeedDataIds.BL04,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 4 — BL1 freight, confirmed (CreditCard) — Demo CL
            new Payment
            {
                Id = SeedDataIds.Payment04,
                PaymentNumber = "PAY-2026-00004",
                PaymentType = "Freight",
                PaymentMethod = "CreditCard",
                Amount = 2850000m,
                TaxAmount = 541500m,
                TotalAmount = 3391500m,
                Currency = "CLP",
                Status = "Confirmed",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 3, 15, 11, 0, 0, DateTimeKind.Utc),
                ConfirmedAt = new DateTime(2026, 3, 15, 11, 2, 0, DateTimeKind.Utc),
                ConfirmedBy = "GATEWAY_AUTO",
                ExternalReference = "CC-TXN-20260315-004",
                ReceiptNumber = "REC-2026-00004",
                ClientId = SeedDataIds.DemoClientCL,
                BillOfLadingId = SeedDataIds.BL01,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 5 — BL2 demurrage, failed (WebPay) — Demo CL
            new Payment
            {
                Id = SeedDataIds.Payment05,
                PaymentNumber = "PAY-2026-00005",
                PaymentType = "Demurrage",
                PaymentMethod = "WebPay",
                Amount = 330000m,
                TaxAmount = 62700m,
                TotalAmount = 392700m,
                Currency = "CLP",
                Status = "Failed",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 4, 5, 16, 0, 0, DateTimeKind.Utc),
                ExternalReference = "WBP-TXN-20260405-FAIL",
                ClientId = SeedDataIds.DemoClientCL,
                BillOfLadingId = SeedDataIds.BL02,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 6 — BL5 freight, cancelled — Demo BO
            new Payment
            {
                Id = SeedDataIds.Payment06,
                PaymentNumber = "PAY-2026-00006",
                PaymentType = "Freight",
                PaymentMethod = "BankTransfer",
                Amount = 4500m,
                TaxAmount = 585m,
                TotalAmount = 5085m,
                Currency = "BOB",
                ExchangeRate = 6.91m,
                Status = "Cancelled",
                Country = CountryCodes.Bolivia,
                PaymentDate = new DateTime(2026, 3, 28, 8, 0, 0, DateTimeKind.Utc),
                ClientId = SeedDataIds.DemoClientBO,
                BillOfLadingId = SeedDataIds.BL05,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 7 — BL2 local charges, processing (WebPay) — Demo CL
            new Payment
            {
                Id = SeedDataIds.Payment07,
                PaymentNumber = "PAY-2026-00007",
                PaymentType = "LocalCharges",
                PaymentMethod = "WebPay",
                Amount = 480000m,
                TaxAmount = 91200m,
                TotalAmount = 571200m,
                Currency = "CLP",
                Status = "Processing",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 4, 12, 9, 30, 0, DateTimeKind.Utc),
                ExternalReference = "WBP-TXN-20260412-007",
                ClientId = SeedDataIds.DemoClientCL,
                BillOfLadingId = SeedDataIds.BL02,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 8 — BL3 freight, confirmed (BankTransfer) — Agent CL
            new Payment
            {
                Id = SeedDataIds.Payment08,
                PaymentNumber = "PAY-2026-00008",
                PaymentType = "Freight",
                PaymentMethod = "BankTransfer",
                Amount = 1750000m,
                TaxAmount = 332500m,
                TotalAmount = 2082500m,
                Currency = "CLP",
                Status = "Confirmed",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 2, 18, 15, 0, 0, DateTimeKind.Utc),
                ConfirmedAt = new DateTime(2026, 2, 19, 10, 0, 0, DateTimeKind.Utc),
                ConfirmedBy = "admin@hapag-lloyd.cl",
                ReceiptNumber = "REC-2026-00008",
                ClientId = SeedDataIds.AgentClientCL,
                BillOfLadingId = SeedDataIds.BL03,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // PAYMENT DETAILS (10 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<PaymentDetail>().HasData(
            // Payment 1 detail (BL3 THC)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail01, ConceptType = "THC", Description = "Terminal Handling Charge - 40OT", Amount = 210000m, Currency = "CLP", TaxAmount = 39900m, PaymentId = SeedDataIds.Payment01 },
            // Payment 2 details (BL1 THC + BL_FEE)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail02, ConceptType = "THC", Description = "Terminal Handling Charge - 40HC", Amount = 185000m, Currency = "CLP", TaxAmount = 35150m, PaymentId = SeedDataIds.Payment02 },
            new PaymentDetail { Id = SeedDataIds.PaymentDetail03, ConceptType = "BL_FEE", Description = "BL Documentation Fee", Amount = 45000m, Currency = "CLP", TaxAmount = 8550m, PaymentId = SeedDataIds.Payment02 },
            // Payment 3 details (BL4 THC + Transit Fee)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail04, ConceptType = "THC", Description = "Terminal Handling Charge - 20DV (Arica)", Amount = 1280m, Currency = "BOB", TaxAmount = 166.40m, PaymentId = SeedDataIds.Payment03 },
            new PaymentDetail { Id = SeedDataIds.PaymentDetail05, ConceptType = "TRANSIT_FEE", Description = "Bolivia Transit Documentation Fee", Amount = 690m, Currency = "BOB", TaxAmount = 89.70m, PaymentId = SeedDataIds.Payment03 },
            // Payment 4 detail (BL1 Freight)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail06, ConceptType = "Freight", Description = "Ocean Freight - Shanghai to Valparaiso", Amount = 2850000m, Currency = "CLP", TaxAmount = 541500m, PaymentId = SeedDataIds.Payment04 },
            // Payment 5 detail (BL2 Demurrage)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail07, ConceptType = "Demurrage", Description = "Demurrage charges - 8 days", Amount = 330000m, Currency = "CLP", TaxAmount = 62700m, PaymentId = SeedDataIds.Payment05 },
            // Payment 6 detail (BL5 Freight BO)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail08, ConceptType = "Freight", Description = "Ocean Freight - Santos to Arica", Amount = 4500m, Currency = "BOB", TaxAmount = 585m, PaymentId = SeedDataIds.Payment06 },
            // Payment 7 details (BL2 Local Charges)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail09, ConceptType = "THC", Description = "Terminal Handling Charge - 20DV", Amount = 185000m, Currency = "CLP", TaxAmount = 35150m, PaymentId = SeedDataIds.Payment07 },
            new PaymentDetail { Id = SeedDataIds.PaymentDetail10, ConceptType = "THC_RF", Description = "Reefer THC Surcharge", Amount = 295000m, Currency = "CLP", TaxAmount = 56050m, PaymentId = SeedDataIds.Payment07 },
            // Payment 8 detail (BL3 Freight)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail11, ConceptType = "Freight", Description = "Flete marítimo BL HLCUVAL250300789", Amount = 1750000m, Currency = "CLP", TaxAmount = 332500m, PaymentId = SeedDataIds.Payment08 });

        // ──────────────────────────────────────────────────────────────
        // CREDIT CLIENTS (4 total: various statuses)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<CreditClient>().HasData(
            // Agent CL — Approved, high limit
            new CreditClient
            {
                Id = SeedDataIds.CreditClient01,
                Country = CountryCodes.Chile,
                CreditLimit = 50000000m,
                CreditStatus = "Approved",
                ApprovedBy = "admin@hapag-lloyd.cl",
                ApprovedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                ClientId = SeedDataIds.AgentClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Demo CL — PendingApproval
            new CreditClient
            {
                Id = SeedDataIds.CreditClient02,
                Country = CountryCodes.Chile,
                CreditLimit = 15000000m,
                CreditStatus = "PendingApproval",
                ClientId = SeedDataIds.DemoClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Demo BO — Approved, BOB
            new CreditClient
            {
                Id = SeedDataIds.CreditClient03,
                Country = CountryCodes.Bolivia,
                CreditLimit = 350000m,
                CreditStatus = "Approved",
                ApprovedBy = "admin@hapag-lloyd.cl",
                ApprovedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                ClientId = SeedDataIds.DemoClientBO,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Admin — Suspended
            new CreditClient
            {
                Id = SeedDataIds.CreditClient04,
                Country = CountryCodes.Chile,
                CreditLimit = 100000000m,
                CreditStatus = "Suspended",
                ApprovedBy = "admin@hapag-lloyd.cl",
                ApprovedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                ClientId = SeedDataIds.AdminClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // DEMURRAGE EXEMPTIONS (2 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<DemurrageExemption>().HasData(
            new DemurrageExemption
            {
                Id = SeedDataIds.DemurrageExemption01,
                ClientName = "Agencia Marítima del Pacífico Ltda",
                TaxId = "96.555.444-3",
                Country = CountryCodes.Chile,
                Reason = "Acuerdo comercial preferencial — cliente con volumen superior a 500 TEU/año",
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new DemurrageExemption
            {
                Id = SeedDataIds.DemurrageExemption02,
                ClientName = "Comercial Altiplano SRL",
                TaxId = "1023456017",
                Country = CountryCodes.Bolivia,
                Reason = "Exención por carga en tránsito internacional — convenio bilateral CL-BO",
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // WAREHOUSE CHANGES (2 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<WarehouseChange>().HasData(
            new WarehouseChange
            {
                Id = SeedDataIds.WarehouseChange01,
                FromWarehouse = "STI San Antonio - Patio A",
                ToWarehouse = "Bodega Central Santiago",
                Amount = 120000m,
                Currency = "CLP",
                Status = "Approved",
                Country = CountryCodes.Chile,
                BillOfLadingId = SeedDataIds.BL01,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new WarehouseChange
            {
                Id = SeedDataIds.WarehouseChange02,
                FromWarehouse = "TPA Arica - Zona Franca",
                ToWarehouse = "Almacén Aduana La Paz",
                Amount = 850m,
                Currency = "BOB",
                Status = "Pending",
                Country = CountryCodes.Bolivia,
                BillOfLadingId = SeedDataIds.BL04,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // SERVICE ORDERS (3 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<ServiceOrder>().HasData(
            new ServiceOrder
            {
                Id = SeedDataIds.ServiceOrder01,
                OrderNumber = "SO-2026-00001",
                OrderType = "Inspection",
                Status = "Completed",
                Description = "Inspección fitosanitaria contenedor HLXU4455667 — maquinaria industrial procedente de Europa",
                Country = CountryCodes.Chile,
                RequestedAt = new DateTime(2026, 2, 16, 8, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2026, 2, 17, 15, 0, 0, DateTimeKind.Utc),
                BillOfLadingId = SeedDataIds.BL03,
                ClientId = SeedDataIds.AdminClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new ServiceOrder
            {
                Id = SeedDataIds.ServiceOrder02,
                OrderNumber = "SO-2026-00002",
                OrderType = "WarehouseRelease",
                Status = "InProgress",
                Description = "Solicitud de retiro contenedores HLXU1234567 y HLXU7654321 — electrónicos importados",
                Country = CountryCodes.Chile,
                RequestedAt = new DateTime(2026, 4, 8, 10, 0, 0, DateTimeKind.Utc),
                BillOfLadingId = SeedDataIds.BL01,
                ClientId = SeedDataIds.DemoClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new ServiceOrder
            {
                Id = SeedDataIds.ServiceOrder03,
                OrderNumber = "SO-2026-00003",
                OrderType = "TransitDocumentation",
                Status = "Pending",
                Description = "Documentación de tránsito internacional Arica → La Paz para contenedor HLXU8899001",
                Country = CountryCodes.Bolivia,
                RequestedAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                BillOfLadingId = SeedDataIds.BL04,
                ClientId = SeedDataIds.DemoClientBO,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // AUDIT LOGS (5 total — sample activity trail)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<AuditLog>().HasData(
            new AuditLog { Id = SeedDataIds.AuditLog01, EntityName = "Payment", EntityId = SeedDataIds.Payment01.ToString(), Action = "Created", NewValues = "{\"PaymentNumber\":\"PAY-2026-00001\",\"Status\":\"Pending\"}", UserId = SeedDataIds.AdminUser.ToString(), Timestamp = new DateTime(2026, 2, 20, 14, 30, 0, DateTimeKind.Utc) },
            new AuditLog { Id = SeedDataIds.AuditLog02, EntityName = "Payment", EntityId = SeedDataIds.Payment01.ToString(), Action = "Updated", OldValues = "{\"Status\":\"Pending\"}", NewValues = "{\"Status\":\"Confirmed\"}", UserId = "WEBPAY_AUTO", Timestamp = new DateTime(2026, 2, 20, 14, 31, 0, DateTimeKind.Utc) },
            new AuditLog { Id = SeedDataIds.AuditLog03, EntityName = "ServiceOrder", EntityId = SeedDataIds.ServiceOrder01.ToString(), Action = "Created", NewValues = "{\"OrderNumber\":\"SO-2026-00001\",\"OrderType\":\"Inspection\"}", UserId = SeedDataIds.AdminUser.ToString(), Timestamp = new DateTime(2026, 2, 16, 8, 0, 0, DateTimeKind.Utc) },
            new AuditLog { Id = SeedDataIds.AuditLog04, EntityName = "WarehouseChange", EntityId = SeedDataIds.WarehouseChange01.ToString(), Action = "Created", NewValues = "{\"FromWarehouse\":\"STI San Antonio - Patio A\",\"ToWarehouse\":\"Bodega Central Santiago\"}", UserId = SeedDataIds.DemoUserCL.ToString(), Timestamp = new DateTime(2026, 4, 6, 11, 0, 0, DateTimeKind.Utc) },
            new AuditLog { Id = SeedDataIds.AuditLog05, EntityName = "CreditClient", EntityId = SeedDataIds.CreditClient01.ToString(), Action = "Updated", OldValues = "{\"CreditStatus\":\"PendingApproval\"}", NewValues = "{\"CreditStatus\":\"Approved\"}", UserId = SeedDataIds.AdminUser.ToString(), Timestamp = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc) });
    }
}
