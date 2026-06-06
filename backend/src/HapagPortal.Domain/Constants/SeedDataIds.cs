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

    // Audit Logs
    public static readonly Guid AuditLog01 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000001");
    public static readonly Guid AuditLog02 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000002");
    public static readonly Guid AuditLog03 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000003");
    public static readonly Guid AuditLog04 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000004");
    public static readonly Guid AuditLog05 = Guid.Parse("BBBBBBBB-0011-0011-0011-000000000005");
}
