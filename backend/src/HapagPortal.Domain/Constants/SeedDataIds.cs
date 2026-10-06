namespace HapagPortal.Domain.Constants;

/// <summary>
/// Well-known GUIDs for seed data. These must remain stable across migrations.
/// </summary>
public static class SeedDataIds
{
    // Currencies
    public static readonly Guid CurrencyCLP = Guid.Parse("A1B2C3D4-0001-0001-0001-000000000001");
    public static readonly Guid CurrencyBOB = Guid.Parse("A1B2C3D4-0001-0001-0001-000000000002");
    public static readonly Guid CurrencyUSD = Guid.Parse("A1B2C3D4-0001-0001-0001-000000000003");

    // Tax Configurations
    public static readonly Guid TaxIvaCL = Guid.Parse("B2C3D4E5-0002-0002-0002-000000000001");
    public static readonly Guid TaxIvaBO = Guid.Parse("B2C3D4E5-0002-0002-0002-000000000002");

    // Admin Client & User
    public static readonly Guid AdminClient = Guid.Parse("C3D4E5F6-0003-0003-0003-000000000001");
    public static readonly Guid AdminUser = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000001");
    public static readonly Guid AdminUserRole = Guid.Parse("E5F6A7B8-0005-0005-0005-000000000001");

    // FAQs - Chile
    public static readonly Guid FaqCL01 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000001");
    public static readonly Guid FaqCL02 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000002");
    public static readonly Guid FaqCL03 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000003");
    public static readonly Guid FaqCL04 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000004");
    public static readonly Guid FaqCL05 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000005");

    // FAQs - Bolivia
    public static readonly Guid FaqBO01 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000011");
    public static readonly Guid FaqBO02 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000012");
    public static readonly Guid FaqBO03 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000013");
    public static readonly Guid FaqBO04 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000014");
    public static readonly Guid FaqBO05 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000015");

    // FAQs - General
    public static readonly Guid FaqGen01 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000021");
    public static readonly Guid FaqGen02 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000022");
    public static readonly Guid FaqGen03 = Guid.Parse("F6A7B8C9-0006-0006-0006-000000000023");

    // Demo Client (Chile)
    public static readonly Guid DemoClientCL = Guid.Parse("C3D4E5F6-0003-0003-0003-000000000010");
    public static readonly Guid DemoUserCL = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000010");
    public static readonly Guid DemoUserRoleCL = Guid.Parse("E5F6A7B8-0005-0005-0005-000000000010");

    // Demo Client (Bolivia)
    public static readonly Guid DemoClientBO = Guid.Parse("C3D4E5F6-0003-0003-0003-000000000020");
    public static readonly Guid DemoUserBO = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000020");
    public static readonly Guid DemoUserRoleBO = Guid.Parse("E5F6A7B8-0005-0005-0005-000000000020");

    // Agent Client (Chile)
    public static readonly Guid AgentClientCL = Guid.Parse("C3D4E5F6-0003-0003-0003-000000000030");
    public static readonly Guid AgentUserCL = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000030");
    public static readonly Guid AgentUserRoleCL = Guid.Parse("E5F6A7B8-0005-0005-0005-000000000030");

    // Bills of Lading
    public static readonly Guid BL01 = Guid.Parse("11111111-0007-0007-0007-000000000001");
    public static readonly Guid BL02 = Guid.Parse("11111111-0007-0007-0007-000000000002");
    public static readonly Guid BL03 = Guid.Parse("11111111-0007-0007-0007-000000000003");
    public static readonly Guid BL04 = Guid.Parse("11111111-0007-0007-0007-000000000004");
    public static readonly Guid BL05 = Guid.Parse("11111111-0007-0007-0007-000000000005");

    // Containers
    public static readonly Guid Container01 = Guid.Parse("22222222-0008-0008-0008-000000000001");
    public static readonly Guid Container02 = Guid.Parse("22222222-0008-0008-0008-000000000002");
    public static readonly Guid Container03 = Guid.Parse("22222222-0008-0008-0008-000000000003");
    public static readonly Guid Container04 = Guid.Parse("22222222-0008-0008-0008-000000000004");
    public static readonly Guid Container05 = Guid.Parse("22222222-0008-0008-0008-000000000005");
    public static readonly Guid Container06 = Guid.Parse("22222222-0008-0008-0008-000000000006");
    public static readonly Guid Container07 = Guid.Parse("22222222-0008-0008-0008-000000000007");

    // Local Charges
    public static readonly Guid LocalCharge01 = Guid.Parse("33333333-0009-0009-0009-000000000001");
    public static readonly Guid LocalCharge02 = Guid.Parse("33333333-0009-0009-0009-000000000002");
    public static readonly Guid LocalCharge03 = Guid.Parse("33333333-0009-0009-0009-000000000003");
    public static readonly Guid LocalCharge04 = Guid.Parse("33333333-0009-0009-0009-000000000004");
    public static readonly Guid LocalCharge05 = Guid.Parse("33333333-0009-0009-0009-000000000005");
    public static readonly Guid LocalCharge06 = Guid.Parse("33333333-0009-0009-0009-000000000006");
    public static readonly Guid LocalCharge07 = Guid.Parse("33333333-0009-0009-0009-000000000007");
    public static readonly Guid LocalCharge08 = Guid.Parse("33333333-0009-0009-0009-000000000008");

    // Demurrage Charges
    public static readonly Guid Demurrage01 = Guid.Parse("44444444-000A-000A-000A-000000000001");
    public static readonly Guid Demurrage02 = Guid.Parse("44444444-000A-000A-000A-000000000002");
    public static readonly Guid Demurrage03 = Guid.Parse("44444444-000A-000A-000A-000000000003");

    // Payments
    public static readonly Guid Payment01 = Guid.Parse("55555555-000B-000B-000B-000000000001");
    public static readonly Guid Payment02 = Guid.Parse("55555555-000B-000B-000B-000000000002");
    public static readonly Guid Payment03 = Guid.Parse("55555555-000B-000B-000B-000000000003");
    public static readonly Guid Payment04 = Guid.Parse("55555555-000B-000B-000B-000000000004");
    public static readonly Guid Payment05 = Guid.Parse("55555555-000B-000B-000B-000000000005");
    public static readonly Guid Payment06 = Guid.Parse("55555555-000B-000B-000B-000000000006");
    public static readonly Guid Payment07 = Guid.Parse("55555555-000B-000B-000B-000000000007");
    public static readonly Guid Payment08 = Guid.Parse("55555555-000B-000B-000B-000000000008");

    // Payment Details
    public static readonly Guid PaymentDetail01 = Guid.Parse("66666666-000C-000C-000C-000000000001");
    public static readonly Guid PaymentDetail02 = Guid.Parse("66666666-000C-000C-000C-000000000002");
    public static readonly Guid PaymentDetail03 = Guid.Parse("66666666-000C-000C-000C-000000000003");
    public static readonly Guid PaymentDetail04 = Guid.Parse("66666666-000C-000C-000C-000000000004");
    public static readonly Guid PaymentDetail05 = Guid.Parse("66666666-000C-000C-000C-000000000005");
    public static readonly Guid PaymentDetail06 = Guid.Parse("66666666-000C-000C-000C-000000000006");
    public static readonly Guid PaymentDetail07 = Guid.Parse("66666666-000C-000C-000C-000000000007");
    public static readonly Guid PaymentDetail08 = Guid.Parse("66666666-000C-000C-000C-000000000008");
    public static readonly Guid PaymentDetail09 = Guid.Parse("66666666-000C-000C-000C-000000000009");
    public static readonly Guid PaymentDetail10 = Guid.Parse("66666666-000C-000C-000C-000000000010");
    public static readonly Guid PaymentDetail11 = Guid.Parse("66666666-000C-000C-000C-000000000011");

    // Credit Clients
    public static readonly Guid CreditClient01 = Guid.Parse("77777777-000D-000D-000D-000000000001");
    public static readonly Guid CreditClient02 = Guid.Parse("77777777-000D-000D-000D-000000000002");
    public static readonly Guid CreditClient03 = Guid.Parse("77777777-000D-000D-000D-000000000003");
    public static readonly Guid CreditClient04 = Guid.Parse("77777777-000D-000D-000D-000000000004");

    // Demurrage Exemptions
    public static readonly Guid DemurrageExemption01 = Guid.Parse("88888888-000E-000E-000E-000000000001");
    public static readonly Guid DemurrageExemption02 = Guid.Parse("88888888-000E-000E-000E-000000000002");

    // Warehouse Changes
    public static readonly Guid WarehouseChange01 = Guid.Parse("99999999-000F-000F-000F-000000000001");
    public static readonly Guid WarehouseChange02 = Guid.Parse("99999999-000F-000F-000F-000000000002");

    // Service Orders
    public static readonly Guid ServiceOrder01 = Guid.Parse("AAAAAAAA-0010-0010-0010-000000000001");
    public static readonly Guid ServiceOrder02 = Guid.Parse("AAAAAAAA-0010-0010-0010-000000000002");
    public static readonly Guid ServiceOrder03 = Guid.Parse("AAAAAAAA-0010-0010-0010-000000000003");

    // Fase 1 Ola A — organizaciones, usuarios y embarques de demostración
    public static readonly Guid PacificTradingClient = Guid.Parse("C3D4E5F6-0003-0003-0003-000000000040");
    public static readonly Guid PendingOrgClient = Guid.Parse("C3D4E5F6-0003-0003-0003-000000000050");
    public static readonly Guid DemoViewerUserCL = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000011");
    public static readonly Guid DemoJoinRequestUserCL = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000012");
    public static readonly Guid PendingOrgUser = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000050");
    public static readonly Guid BL06 = Guid.Parse("11111111-0007-0007-0007-000000000006");
    public static readonly Guid BL07 = Guid.Parse("11111111-0007-0007-0007-000000000007");
    public static readonly Guid BL08 = Guid.Parse("11111111-0007-0007-0007-000000000008");
    public static readonly Guid Container08 = Guid.Parse("22222222-0008-0008-0008-000000000008");
    public static readonly Guid Container09 = Guid.Parse("22222222-0008-0008-0008-000000000009");
    public static readonly Guid Container10 = Guid.Parse("22222222-0008-0008-0008-000000000010");
    public static readonly Guid LocalCharge09 = Guid.Parse("33333333-0009-0009-0009-000000000009");
    public static readonly Guid LocalCharge10 = Guid.Parse("33333333-0009-0009-0009-000000000010");
    public static readonly Guid LocalCharge11 = Guid.Parse("33333333-0009-0009-0009-000000000011");
    public static readonly Guid LocalCharge12 = Guid.Parse("33333333-0009-0009-0009-000000000012");
    public static readonly Guid ServiceOrder04 = Guid.Parse("AAAAAAAA-0010-0010-0010-000000000004");

    // Fase 1 Ola B — accesos a terceros de demostración
    public static readonly Guid DemoAccessGrant01 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000001");
    public static readonly Guid DemoAccessGrant02 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000002");
    public static readonly Guid DemoDefaultGrantee01 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000011");
    public static readonly Guid DemoOpenAccessSetting01 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000021");
    public static readonly Guid DemoAccessAudit01 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000031");
    public static readonly Guid DemoAccessAudit02 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000032");
    public static readonly Guid DemoAccessAudit03 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000033");
    public static readonly Guid DemoAccessAudit04 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000034");
    public static readonly Guid DemoAccessAudit05 = Guid.Parse("CCCCCCCC-0012-0012-0012-000000000035");

    // Fase 1 Ola C — reglas de cobro, tarifas y demurrage por estado
    public static readonly Guid CreditDemoClient = Guid.Parse("C3D4E5F6-0003-0003-0003-000000000060");
    public static readonly Guid CreditDemoUser = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000060");
    public static readonly Guid FfwwDemoClient = Guid.Parse("C3D4E5F6-0003-0003-0003-000000000070");
    public static readonly Guid FfwwDemoUser = Guid.Parse("D4E5F6A7-0004-0004-0004-000000000070");
    public static readonly Guid BL09 = Guid.Parse("11111111-0007-0007-0007-000000000009");
    public static readonly Guid BL10 = Guid.Parse("11111111-0007-0007-0007-000000000010");
    public static readonly Guid BL11 = Guid.Parse("11111111-0007-0007-0007-000000000011");
    public static readonly Guid Container11 = Guid.Parse("22222222-0008-0008-0008-000000000011");
    public static readonly Guid Container12 = Guid.Parse("22222222-0008-0008-0008-000000000012");
    public static readonly Guid Container13 = Guid.Parse("22222222-0008-0008-0008-000000000013");
    public static readonly Guid Container14 = Guid.Parse("22222222-0008-0008-0008-000000000014");
    public static readonly Guid BL10MasterConsignee = Guid.Parse("DDDDDDDD-0013-0013-0013-000000000001");
    public static readonly Guid Demurrage04 = Guid.Parse("44444444-000A-000A-000A-000000000004");
    public static readonly Guid LocalCharge13 = Guid.Parse("33333333-0009-0009-0009-000000000013");
    public static readonly Guid LocalCharge14 = Guid.Parse("33333333-0009-0009-0009-000000000014");
    public static readonly Guid LocalCharge15 = Guid.Parse("33333333-0009-0009-0009-000000000015");
    public static readonly Guid LocalCharge16 = Guid.Parse("33333333-0009-0009-0009-000000000016");
    public static readonly Guid LocalCharge17 = Guid.Parse("33333333-0009-0009-0009-000000000017");
    public static readonly Guid LocalCharge18 = Guid.Parse("33333333-0009-0009-0009-000000000018");
    public static readonly Guid LocalCharge19 = Guid.Parse("33333333-0009-0009-0009-000000000019");
    public static readonly Guid LocalCharge20 = Guid.Parse("33333333-0009-0009-0009-000000000020");
    public static readonly Guid LocalCharge21 = Guid.Parse("33333333-0009-0009-0009-000000000021");
    public static readonly Guid LocalCharge22 = Guid.Parse("33333333-0009-0009-0009-000000000022");
    public static readonly Guid TariffKteCL = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000001");
    public static readonly Guid TariffKtfCL = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000002");
    public static readonly Guid TariffWarehouseChangeBO = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000003");
    public static readonly Guid TariffLateArrivalCL = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000004");
    public static readonly Guid TariffDemurrageCL20 = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000005");
    public static readonly Guid TariffDemurrageCL = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000006");
    public static readonly Guid TariffDemurrageBO = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000007");
    public static readonly Guid TariffAdvanceDemurrageBO = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000008");
    public static readonly Guid RuleFreeWarehouseChangeCL = Guid.Parse("EEEEEEEE-0015-0015-0015-000000000001");
    public static readonly Guid RuleAdvanceDemurrageBO = Guid.Parse("EEEEEEEE-0015-0015-0015-000000000002");

    // Fase 1 Ola D — carro, medios y monedas de pago, bloqueos, facturas e historial
    public static readonly Guid CurrencyEUR = Guid.Parse("A1B2C3D4-0001-0001-0001-000000000004");
    public static readonly Guid Payment09 = Guid.Parse("55555555-000B-000B-000B-000000000009");
    public static readonly Guid Payment10 = Guid.Parse("55555555-000B-000B-000B-000000000010");
    public static readonly Guid Payment11 = Guid.Parse("55555555-000B-000B-000B-000000000011");
    public static readonly Guid Payment12 = Guid.Parse("55555555-000B-000B-000B-000000000012");
    public static readonly Guid Payment13 = Guid.Parse("55555555-000B-000B-000B-000000000013");
    public static readonly Guid BlockWindowPast = Guid.Parse("FFFFFFFF-0016-0016-0016-000000000001");
    public static readonly Guid BlockWindowFuture = Guid.Parse("FFFFFFFF-0016-0016-0016-000000000002");
    public static readonly Guid BlockWindowFutureBO = Guid.Parse("FFFFFFFF-0016-0016-0016-000000000003");
    public static readonly Guid InvoiceDemurrageBL09 = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000001");
    public static readonly Guid InvoiceOverdueBL01 = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000002");
    public static readonly Guid InvoicePaidBL02 = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000003");
    public static readonly Guid InvoiceNoFolioBL06 = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000004");
    public static readonly Guid InvoiceCreditNoteBL01 = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000005");
    public static readonly Guid InvoiceEurBL10 = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000006");
    public static readonly Guid InvoicePendingBO = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000007");
    public static readonly Guid InvoicePaidBO = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000008");
    public static readonly Guid InvoiceCreditCustomer01 = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000009");
    public static readonly Guid InvoiceCreditCustomer02 = Guid.Parse("FFFFFFFF-0017-0017-0017-000000000010");

    // Fase 1 Ola E — documentos del embarque, BL de Collect para la agencia y BL del FFWW con carta
    public static readonly Guid BL12 = Guid.Parse("11111111-0007-0007-0007-000000000012");
    public static readonly Guid BL13 = Guid.Parse("11111111-0007-0007-0007-000000000013");
    public static readonly Guid Container15 = Guid.Parse("22222222-0008-0008-0008-000000000015");
    public static readonly Guid Container16 = Guid.Parse("22222222-0008-0008-0008-000000000016");
    public static readonly Guid LocalCharge23 = Guid.Parse("33333333-0009-0009-0009-000000000023");
    public static readonly Guid LocalCharge24 = Guid.Parse("33333333-0009-0009-0009-000000000024");
    public static readonly Guid LocalCharge25 = Guid.Parse("33333333-0009-0009-0009-000000000025");
    public static readonly Guid TariffTransshipmentCL = Guid.Parse("EEEEEEEE-0014-0014-0014-000000000009");
    public static readonly Guid DocumentTransshipmentBL06 = Guid.Parse("FFFFFFFF-0018-0018-0018-000000000001");
    public static readonly Guid DocumentBlCopyBL01 = Guid.Parse("FFFFFFFF-0018-0018-0018-000000000002");
    public static readonly Guid DocumentCollectBL12 = Guid.Parse("FFFFFFFF-0018-0018-0018-000000000003");
    public static readonly Guid DocumentLetterBL13 = Guid.Parse("FFFFFFFF-0018-0018-0018-000000000004");

    // Fase 1 Ola F — reglas de publicación por DIFU (M2-01) y BL de demostración con destino final distinto
    public static readonly Guid BL14 = Guid.Parse("11111111-0007-0007-0007-000000000014");
    public static readonly Guid BL15 = Guid.Parse("11111111-0007-0007-0007-000000000015");
    public static readonly Guid Container17 = Guid.Parse("22222222-0008-0008-0008-000000000017");
    public static readonly Guid Container18 = Guid.Parse("22222222-0008-0008-0008-000000000018");
    public static readonly Guid LocalCharge26 = Guid.Parse("33333333-0009-0009-0009-000000000026");
    public static readonly Guid PublicationRuleAntofagasta = Guid.Parse("FFFFFFFF-0019-0019-0019-000000000001");
    public static readonly Guid PublicationRulePuntaArenas = Guid.Parse("FFFFFFFF-0019-0019-0019-000000000002");

    // Fase 2 Ola G — servicios on demand (M2-03, M2-04, M3-07 a M3-15) e historial de cambio de almacén (M3-06)
    public static readonly Guid BL16 = Guid.Parse("11111111-0007-0007-0007-000000000016");
    public static readonly Guid BL17 = Guid.Parse("11111111-0007-0007-0007-000000000017");
    public static readonly Guid Container19 = Guid.Parse("22222222-0008-0008-0008-000000000019");
    public static readonly Guid Container20 = Guid.Parse("22222222-0008-0008-0008-000000000020");
    public static readonly Guid Container21 = Guid.Parse("22222222-0008-0008-0008-000000000021");
    public static readonly Guid Container22 = Guid.Parse("22222222-0008-0008-0008-000000000022");
    public static readonly Guid WarehouseChange03 = Guid.Parse("99999999-000F-000F-000F-000000000003");
    public static readonly Guid WarehouseChange04 = Guid.Parse("99999999-000F-000F-000F-000000000004");
    public static readonly Guid WarehouseChangeBatch01 = Guid.Parse("99999999-0020-0020-0020-000000000001");
    public static readonly Guid Payment14 = Guid.Parse("55555555-000B-000B-000B-000000000014");
    public static readonly Guid ServiceRequestDropOffPending = Guid.Parse("FFFFFFFF-0021-0021-0021-000000000001");
    public static readonly Guid ServiceRequestSealsPendingPayment = Guid.Parse("FFFFFFFF-0021-0021-0021-000000000002");
    public static readonly Guid ServiceRequestCorrectionInProgress = Guid.Parse("FFFFFFFF-0021-0021-0021-000000000003");
    public static readonly Guid ServiceRequestBlHouseCompleted = Guid.Parse("FFFFFFFF-0021-0021-0021-000000000004");
    public static readonly Guid ServiceRequestDropOffRejected = Guid.Parse("FFFFFFFF-0021-0021-0021-000000000005");
    public static readonly Guid ServiceRequestLateArrivalCancelled = Guid.Parse("FFFFFFFF-0021-0021-0021-000000000006");
    public static readonly Guid ServiceRequestXomExempt = Guid.Parse("FFFFFFFF-0021-0021-0021-000000000007");
    public static readonly Guid ServiceRequestMatrixDraft = Guid.Parse("FFFFFFFF-0021-0021-0021-000000000008");
    public static readonly Guid LocalChargeSealsBL06 = Guid.Parse("33333333-0009-0009-0009-000000000027");
    public static readonly Guid LocalChargeCorrectionBL02 = Guid.Parse("33333333-0009-0009-0009-000000000028");
    public static readonly Guid LocalChargeBlHouseBL16 = Guid.Parse("33333333-0009-0009-0009-000000000029");

    // Audit Logs
    public static readonly Guid AuditLog01 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000001");
    public static readonly Guid AuditLog02 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000002");
    public static readonly Guid AuditLog03 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000003");
    public static readonly Guid AuditLog04 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000004");
    public static readonly Guid AuditLog05 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000005");
}
