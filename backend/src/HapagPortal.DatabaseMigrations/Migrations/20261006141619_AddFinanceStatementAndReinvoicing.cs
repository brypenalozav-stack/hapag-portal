using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceStatementAndReinvoicing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByInvoiceId",
                table: "CustomerInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesInvoiceId",
                table: "CustomerInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChargeSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PayerOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerTaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PayerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OnBehalfOfOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillingTaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BillingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BookingNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ItemType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidCurrency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiptDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchedInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MatchedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MatchNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeSettlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditImputationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NexusCreditConcept = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditImputationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepositProofs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankReference = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    DepositDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositProofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositProofs_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceReissues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalSiiNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OriginalSourceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OriginalTaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OriginalLegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VatLossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FeeTaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    AcceptorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AcceptanceStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AcceptanceTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AcceptanceRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptanceExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcceptedByTaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AcceptedFromAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeclinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeclineReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NewInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceReissues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceReissues_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000018"), "HLCUSAI260701810", null, "HLCUBKG2607181", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Lima Foods SAC", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "PELIM", 1900m, "USD", new DateTime(2026, 9, 29, 15, 0, 0, 0, DateTimeKind.Utc), null, null, false, false, "Issued", new DateTime(2026, 10, 3, 6, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Lima, Peru", "Callao (PECLL)", "PECLL", "San Antonio (CLSAI)", "Export", "Importadora Demo SpA", "Departed", "BL", "Valparaiso Express", null, "2610N" },
                    { new Guid("11111111-0007-0007-0007-000000000019"), "HLCUVAP260601930", null, "HLCUBKG2606193", new Guid("c3d4e5f6-0003-0003-0003-000000000060"), "Distribuidora Andes Crédito SpA", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Utc), null, "CLVAP", 2400m, "USD", new DateTime(2026, 9, 5, 12, 0, 0, 0, DateTimeKind.Utc), null, null, false, false, "Issued", new DateTime(2026, 9, 6, 10, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Santiago, Chile", "Valparaiso (CLVAP)", "CLVAP", "Santos (BRSSZ)", "Import", "Santos Coffee Exporters Ltda", "Arrived", "BL", "Santos Express", null, "2610N" }
                });

            migrationBuilder.InsertData(
                table: "ChargeConcepts",
                columns: new[] { "Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff" },
                values: new object[,]
                {
                    { new Guid("51c3d02a-634f-2135-64c7-0c59a15165fb"), "Service", "VAT_LOSS", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 320, true, null, null, "Pérdida de IVA", false, false },
                    { new Guid("923fac6f-9222-dbf9-90ea-4f267bafce41"), "Service", "REINVOICING", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 310, true, null, null, "Refacturación IAO", false, false }
                });

            migrationBuilder.InsertData(
                table: "ChargeSettlements",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "Currency", "Description", "ItemType", "Kind", "MatchNote", "MatchedAt", "MatchedBy", "MatchedInvoiceId", "OnBehalfOfOrganizationId", "PaidAmount", "PaidCurrency", "PayerName", "PayerOrganizationId", "PayerTaxId", "PaymentDetailId", "PaymentId", "PaymentNumber", "ReceiptDocumentId", "ReceiptNumber", "SettledAt", "SourceId", "Status" },
                values: new object[,]
                {
                    { new Guid("1f978f39-3168-a413-0edc-19582df4a4e7"), 71400m, new Guid("11111111-0007-0007-0007-000000000018"), "Agencia Marítima del Pacífico Ltda", "96555444-3", "HLCUSAI260701810", "HLCUBKG2607181", "GATE_OUT", "CL", new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), "CLP", "Gate Out - 40HC (San Antonio)", "LocalCharge", "Advance", null, new DateTime(2026, 10, 4, 12, 0, 0, 0, DateTimeKind.Utc), "INVOICE_REFRESH", new Guid("ffffffff-0017-0017-0017-000000000014"), null, 71400m, "CLP", "Agencia Marítima del Pacífico Ltda", new Guid("c3d4e5f6-0003-0003-0003-000000000030"), "96555444-3", new Guid("0745ba3a-34ac-c833-5db6-bf116971a424"), new Guid("55555555-000b-000b-000b-000000000017"), "PAY-20261001-D4E5F6A7", new Guid("ffffffff-0018-0018-0018-000000000005"), "RCP-20261001-E8F9A0B1", new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-0009-0009-0009-000000000034"), "Matched" },
                    { new Guid("3302d5bd-8631-c56a-7b09-37d0629b2c35"), 53550m, new Guid("11111111-0007-0007-0007-000000000019"), "Distribuidora Andes Crédito SpA", "76000002-2", "HLCUVAP260601930", "HLCUBKG2606193", "BL_FEE", "CL", new DateTime(2026, 10, 2, 13, 0, 0, 0, DateTimeKind.Utc), "CLP", "BL Documentation Fee (import)", "LocalCharge", "Advance", null, null, null, null, null, 53550m, "CLP", "Distribuidora Andes Crédito SpA", new Guid("c3d4e5f6-0003-0003-0003-000000000060"), "76000002-2", new Guid("db2bf9c7-ceb5-f5a2-e768-8501f7664a50"), new Guid("55555555-000b-000b-000b-000000000015"), "PAY-20261002-A1C2E3F4", null, "RCP-20261002-B5D6E7F8", new DateTime(2026, 10, 2, 13, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-0009-0009-0009-000000000030"), "Open" },
                    { new Guid("8b700f9d-0925-04e9-55c9-7eefbc8e15fc"), 29750m, new Guid("11111111-0007-0007-0007-000000000019"), "Distribuidora Andes Crédito SpA", "76000002-2", "HLCUVAP260601930", "HLCUBKG2606193", "ISPS", "CL", new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), "CLP", "ISPS", "LocalCharge", "CreditImputation", null, null, null, null, null, 29750m, "CLP", "Distribuidora Andes Crédito SpA", new Guid("c3d4e5f6-0003-0003-0003-000000000060"), "76000002-2", new Guid("aab31ddd-35ff-069a-d97b-e746504c1cf4"), new Guid("55555555-000b-000b-000b-000000000016"), "CRI-20261003-C9D8E7F6", null, null, new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), new Guid("33333333-0009-0009-0009-000000000031"), "Open" }
                });

            migrationBuilder.InsertData(
                table: "ConfigurationSettings",
                columns: new[] { "Id", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Key", "ModifiedAt", "ModifiedBy", "Scope", "Value" },
                values: new object[,]
                {
                    { new Guid("11f1dde1-1820-e7fe-1e07-4aa557ecd21c"), null, new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Statement.AgingBuckets", null, null, "Global", "30,60,90" },
                    { new Guid("faff8f22-0d64-9389-8e15-d248dd2301b5"), null, new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Statement.DueSoonDays", null, null, "Global", "7" }
                });

            migrationBuilder.InsertData(
                table: "CreditImputationRules",
                columns: new[] { "Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy", "NexusCreditConcept", "Notes" },
                values: new object[,]
                {
                    { new Guid("0016cb8a-acd8-7248-7612-82d73aa4d31f"), "GATE_OUT", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "LOCAL_CHARGES", "Propuesta conservadora (recargos locales de Chile) pendiente de confirmación de Finanzas (M5-10)." },
                    { new Guid("3a4b4010-b87c-a88e-d52a-b2d36a834765"), "THC", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "LOCAL_CHARGES", "Propuesta conservadora (recargos locales de Chile) pendiente de confirmación de Finanzas (M5-10)." },
                    { new Guid("5b866cc3-d88e-fc09-72a4-460533ba92bd"), "BL_FEE", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "LOCAL_CHARGES", "Propuesta conservadora (recargos locales de Chile) pendiente de confirmación de Finanzas (M5-10)." },
                    { new Guid("893a8a68-2509-dfec-e30f-9131e821fc5e"), "ISPS", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "LOCAL_CHARGES", "Propuesta conservadora (recargos locales de Chile) pendiente de confirmación de Finanzas (M5-10)." }
                });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000001"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000002"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000003"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000004"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000005"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000006"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000007"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000008"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000009"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000010"),
                columns: new[] { "SupersededByInvoiceId", "SupersedesInvoiceId" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "CustomerInvoices",
                columns: new[] { "Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SupersededByInvoiceId", "SupersedesInvoiceId", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0017-0017-0017-000000000011"), new Guid("11111111-0007-0007-0007-000000000011"), "HLCUVAP260401130", "HLCUBKG2604113", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Invoice", new DateOnly(2026, 7, 1), true, new DateOnly(2026, 6, 1), "Distribuidora Andes Crédito SpA", null, null, 80000m, new Guid("c3d4e5f6-0003-0003-0003-000000000060"), null, null, "100120", "ACCEPTED", "DUMMY", "HL-CL-2026-003410", "Pending", null, null, new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 15200m, "76000002-2", 95200m },
                    { new Guid("ffffffff-0017-0017-0017-000000000012"), new Guid("11111111-0007-0007-0007-000000000011"), "HLCUVAP260401130", "HLCUBKG2604113", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Invoice", new DateOnly(2026, 8, 3), true, new DateOnly(2026, 7, 20), "Distribuidora Andes Crédito SpA", null, null, 150000m, new Guid("c3d4e5f6-0003-0003-0003-000000000060"), null, null, "100190", "ACCEPTED", "DUMMY", "HL-CL-2026-003702", "Pending", null, null, new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 28500m, "76000002-2", 178500m },
                    { new Guid("ffffffff-0017-0017-0017-000000000013"), new Guid("11111111-0007-0007-0007-000000000019"), "HLCUVAP260601930", "HLCUBKG2606193", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Invoice", new DateOnly(2026, 10, 10), true, new DateOnly(2026, 9, 10), "Distribuidora Andes Crédito SpA", null, null, 45000m, new Guid("c3d4e5f6-0003-0003-0003-000000000060"), null, null, "100310", "ACCEPTED", "DUMMY", "HL-CL-2026-004720", "Pending", null, null, new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 8550m, "76000002-2", 53550m },
                    { new Guid("ffffffff-0017-0017-0017-000000000014"), new Guid("11111111-0007-0007-0007-000000000018"), "HLCUSAI260701810", "HLCUBKG2607181", "GATE_OUT", "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Invoice", new DateOnly(2026, 11, 3), false, new DateOnly(2026, 10, 4), "Agencia Marítima del Pacífico Ltda", null, null, 60000m, new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), new Guid("55555555-000b-000b-000b-000000000017"), "100318", "ACCEPTED", "DUMMY", "HL-CL-2026-004790", "Paid", null, null, new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 11400m, "96555444-3", 71400m }
                });

            migrationBuilder.InsertData(
                table: "DepositProofs",
                columns: new[] { "Id", "BankName", "BankReference", "ContentHash", "ContentType", "DepositAmount", "DepositDate", "FileName", "Notes", "PaymentId", "RejectionReason", "ReviewNotes", "ReviewedAt", "ReviewedBy", "ReviewedByUserId", "SizeBytes", "Status", "StorageKey", "UploadedAt", "UploadedBy", "UploadedByUserId" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0024-0024-0024-000000000001"), "Banco de Chile", null, null, "image/jpeg", 53550m, new DateOnly(2026, 10, 3), "comprobante-deposito-borroso.jpg", null, new Guid("55555555-000b-000b-000b-000000000011"), "La imagen no permite leer el número de operación ni el monto abonado.", null, new DateTime(2026, 10, 4, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), 0L, "Rejected", null, new DateTime(2026, 10, 3, 18, 0, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010") },
                    { new Guid("ffffffff-0024-0024-0024-000000000002"), "Banco de Chile", "OP-55821473", null, "application/pdf", 53550m, new DateOnly(2026, 10, 3), "comprobante-deposito-BDP-20261003-5C7D9E1F.pdf", "Depósito en efectivo, sucursal Las Condes.", new Guid("55555555-000b-000b-000b-000000000011"), null, null, null, null, null, 0L, "Submitted", null, new DateTime(2026, 10, 4, 15, 10, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010") }
                });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000035"), 25000m, new Guid("11111111-0007-0007-0007-000000000001"), "REINVOICING", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Refacturación de la factura 100198 (SRV-20261005-5E1A0009)", true, null, null, "Pending", 4750m, 19m, 29750m },
                    { new Guid("33333333-0009-0009-0009-000000000036"), 22800m, new Guid("11111111-0007-0007-0007-000000000001"), "VAT_LOSS", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Pérdida de IVA de la factura 100198 (SRV-20261005-5E1A0009)", false, null, null, "Pending", 0m, 0m, 22800m }
                });

            migrationBuilder.InsertData(
                table: "MaintainerChangeLogs",
                columns: new[] { "Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue" },
                values: new object[,]
                {
                    { new Guid("218d2183-e252-8852-de39-cd0104d6a085"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("c7f2abc0-29ae-1d0b-d957-ea4707a567b0"), "Tariff", "{\"conceptCode\":\"REINVOICING\",\"code\":null,\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":null,\"description\":\"Refacturaci\\u00F3n IAO por factura (M3-11)\",\"amount\":25000,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("2d9f6544-3c95-8cf5-f6dd-e808cedd59ce"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("5b866cc3-d88e-fc09-72a4-460533ba92bd"), "CreditImputationRule", "{\"country\":\"CL\",\"conceptCode\":\"BL_FEE\",\"nexusCreditConcept\":\"LOCAL_CHARGES\",\"isEnabled\":true,\"notes\":\"Propuesta conservadora (recargos locales de Chile) pendiente de confirmaci\\u00F3n de Finanzas (M5-10).\"}", null },
                    { new Guid("89c5b237-f1a2-5a8e-335a-e2855cd4e5bb"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0022-0022-0022-000000000001"), "ServiceDefinition", "{\"code\":\"IAO_REINVOICING\",\"nameEs\":\"Refacturaci\\u00F3n IAO y p\\u00E9rdida de IVA\",\"nameEn\":\"IAO re-invoicing and VAT loss\",\"descriptionEs\":\"Refacturaci\\u00F3n de una factura emitida a una nueva raz\\u00F3n social: el cliente registra los nuevos datos de facturaci\\u00F3n y adjunta la aprobaci\\u00F3n de la nueva raz\\u00F3n social; se cobran juntos la refacturaci\\u00F3n y la p\\u00E9rdida de IVA, y la factura se emite solo con la aceptaci\\u00F3n del cobro por la nueva raz\\u00F3n social (M3-11, CL-EXP-11, CL-IMP-09). Se solicita desde la factura.\",\"descriptionEn\":\"Re-invoicing of an issued invoice to a new legal entity: the customer enters the new billing data and attaches the new entity\\u0027s approval; the re-invoicing fee and the VAT loss are paid together, and the invoice is issued only after the new legal entity accepts the charge (M3-11). Requested from the invoice.\",\"operations\":\"IMPORT,EXPORT\",\"countries\":\"CL\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":false,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022newCompanyApproval\\u0022,\\u0022labelEs\\u0022:\\u0022Aprobaci\\\\u00F3n de la nueva raz\\\\u00F3n social\\u0022,\\u0022labelEn\\u0022:\\u0022Approval of the new legal entity\\u0022,\\u0022type\\u0022:\\u0022file\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022reason\\u0022,\\u0022labelEs\\u0022:\\u0022Motivo de la refacturaci\\\\u00F3n\\u0022,\\u0022labelEn\\u0022:\\u0022Reason for the re-invoicing\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":true,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"REINVOICING\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":true,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-on-demand.pay\",\"displayOrder\":120,\"isActive\":true}", null },
                    { new Guid("afa9137e-bf2b-5690-fe33-0cb261464598"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("0016cb8a-acd8-7248-7612-82d73aa4d31f"), "CreditImputationRule", "{\"country\":\"CL\",\"conceptCode\":\"GATE_OUT\",\"nexusCreditConcept\":\"LOCAL_CHARGES\",\"isEnabled\":true,\"notes\":\"Propuesta conservadora (recargos locales de Chile) pendiente de confirmaci\\u00F3n de Finanzas (M5-10).\"}", null },
                    { new Guid("b7fe99ef-e1c5-2eba-49b5-12812d0cc83a"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("893a8a68-2509-dfec-e30f-9131e821fc5e"), "CreditImputationRule", "{\"country\":\"CL\",\"conceptCode\":\"ISPS\",\"nexusCreditConcept\":\"LOCAL_CHARGES\",\"isEnabled\":true,\"notes\":\"Propuesta conservadora (recargos locales de Chile) pendiente de confirmaci\\u00F3n de Finanzas (M5-10).\"}", null },
                    { new Guid("e09a7df1-2e24-226b-f05d-131b4282f10b"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("3a4b4010-b87c-a88e-d52a-b2d36a834765"), "CreditImputationRule", "{\"country\":\"CL\",\"conceptCode\":\"THC\",\"nexusCreditConcept\":\"LOCAL_CHARGES\",\"isEnabled\":true,\"notes\":\"Propuesta conservadora (recargos locales de Chile) pendiente de confirmaci\\u00F3n de Finanzas (M5-10).\"}", null }
                });

            migrationBuilder.InsertData(
                table: "ServiceDefinitions",
                columns: new[] { "Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule" },
                values: new object[] { new Guid("ffffffff-0022-0022-0022-000000000001"), "local-charges-on-demand.pay", true, "None", "Always", true, "REINVOICING", "IAO_REINVOICING", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Re-invoicing of an issued invoice to a new legal entity: the customer enters the new billing data and attaches the new entity's approval; the re-invoicing fee and the VAT loss are paid together, and the invoice is issued only after the new legal entity accepts the charge (M3-11). Requested from the invoice.", "Refacturación de una factura emitida a una nueva razón social: el cliente registra los nuevos datos de facturación y adjunta la aprobación de la nueva razón social; se cobran juntos la refacturación y la pérdida de IVA, y la factura se emite solo con la aceptación del cobro por la nueva razón social (M3-11, CL-EXP-11, CL-IMP-09). Se solicita desde la factura.", 120, false, null, "None", "[{\"key\":\"newCompanyApproval\",\"labelEs\":\"Aprobaci\\u00F3n de la nueva raz\\u00F3n social\",\"labelEn\":\"Approval of the new legal entity\",\"type\":\"file\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"reason\",\"labelEs\":\"Motivo de la refacturaci\\u00F3n\",\"labelEn\":\"Reason for the re-invoicing\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "None", 0, null, null, "IAO re-invoicing and VAT loss", "Refacturación IAO y pérdida de IVA", "IMPORT,EXPORT", "Tariff", "PerRequest", "BL", null, false, false, true, null, true, "None" });

            migrationBuilder.InsertData(
                table: "Tariffs",
                columns: new[] { "Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo" },
                values: new object[] { new Guid("c7f2abc0-29ae-1d0b-d957-ea4707a567b0"), 25000m, null, "REINVOICING", null, "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Refacturación IAO por factura (M3-11)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000023"), new Guid("11111111-0007-0007-0007-000000000018"), "HLXU3071801", "40HC", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-071801", "OnBoard", null, null, 24800m },
                    { new Guid("22222222-0008-0008-0008-000000000024"), new Guid("11111111-0007-0007-0007-000000000019"), "HLXU3061901", "20DV", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-061901", "Discharged", null, null, 17600m }
                });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000030"), 45000m, new Guid("11111111-0007-0007-0007-000000000019"), "BL_FEE", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "BL Documentation Fee (import)", true, null, null, "Paid", 8550m, 19m, 53550m },
                    { new Guid("33333333-0009-0009-0009-000000000031"), 25000m, new Guid("11111111-0007-0007-0007-000000000019"), "ISPS", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "ISPS", true, null, null, "CreditImputed", 4750m, 19m, 29750m },
                    { new Guid("33333333-0009-0009-0009-000000000032"), 185000m, new Guid("11111111-0007-0007-0007-000000000019"), "THC", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 20DV (Valparaíso)", true, null, null, "Pending", 35150m, 19m, 220150m },
                    { new Guid("33333333-0009-0009-0009-000000000033"), 60000m, new Guid("11111111-0007-0007-0007-000000000019"), "GATE_OUT", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Gate Out - 20DV (Valparaíso)", true, null, null, "Pending", 11400m, 19m, 71400m },
                    { new Guid("33333333-0009-0009-0009-000000000034"), 60000m, new Guid("11111111-0007-0007-0007-000000000018"), "GATE_OUT", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Gate Out - 40HC (San Antonio)", true, null, null, "Paid", 11400m, 19m, 71400m }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("55555555-000b-000b-000b-000000000015"), null, 45000m, new Guid("11111111-0007-0007-0007-000000000019"), null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000060"), new DateTime(2026, 10, 2, 13, 0, 0, 0, DateTimeKind.Utc), "BANCOCHILE_WEBHOOK", "CL", new DateTime(2026, 10, 2, 12, 58, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000060", new Guid("d4e5f6a7-0004-0004-0004-000000000060"), "CLP", null, null, null, null, "PAY-20261002-A1C2E3F4", null, null, null, null, null, "Account", "Distribuidora Andes Crédito SpA", "76000002-2", new DateTime(2026, 10, 2, 13, 0, 0, 0, DateTimeKind.Utc), "BANK_BUTTON_BCH", "BANK_BUTTON_BCH", "PAY-20261002-A1C2E3F4", "Account", "BancoChile", "DUMMY-BANCOCHILE-PAY-20261002-A1C2E3F4", "BCH-TXN-55100877", "RCP-20261002-B5D6E7F8", null, null, null, null, "Confirmed", new DateTime(2026, 10, 2, 13, 0, 0, 0, DateTimeKind.Utc), 8550m, 53550m },
                    { new Guid("55555555-000b-000b-000b-000000000016"), null, 25000m, new Guid("11111111-0007-0007-0007-000000000019"), null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000060"), new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), "credito@distribuidoraandes.cl", "CL", new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000060", new Guid("d4e5f6a7-0004-0004-0004-000000000060"), "CLP", null, null, null, null, "CRI-20261003-C9D8E7F6", null, null, null, null, null, "CreditLine", "Distribuidora Andes Crédito SpA", "76000002-2", new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), "CREDIT_LINE", "CREDIT_LINE", "CRI-20261003-C9D8E7F6", "CreditLine", null, null, null, null, null, null, null, null, "Confirmed", new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), 4750m, 29750m },
                    { new Guid("55555555-000b-000b-000b-000000000017"), null, 60000m, new Guid("11111111-0007-0007-0007-000000000018"), null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), "KHIPU_WEBHOOK", "CL", new DateTime(2026, 10, 1, 13, 57, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000030", new Guid("d4e5f6a7-0004-0004-0004-000000000030"), "CLP", null, null, null, null, "PAY-20261001-D4E5F6A7", null, null, null, null, null, "Cart", "Agencia Marítima del Pacífico Ltda", "96555444-3", new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), "KHIPU", "KHIPU", "PAY-20261001-D4E5F6A7", "Cart", "Khipu", "DUMMY-KHIPU-PAY-20261001-D4E5F6A7", "KHP-TXN-8813901", "RCP-20261001-E8F9A0B1", null, null, null, null, "Confirmed", new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), 11400m, 71400m }
                });

            migrationBuilder.InsertData(
                table: "ServiceRequests",
                columns: new[] { "Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount" },
                values: new object[] { new Guid("ffffffff-0021-0021-0021-000000000009"), null, 47800m, null, null, new Guid("11111111-0007-0007-0007-000000000001"), "Comercio al por mayor", "Av. Libertad 1405, Viña del Mar", "facturacion@comercialaustral.cl", "Comercial Austral SpA", "77888999-1", "HLCUVAL250100123", "HLCUBKG2501001", null, "REINVOICING", null, null, "CL", new DateTime(2026, 10, 5, 15, 40, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", "CLP", "IAO_REINVOICING", new Guid("ffffffff-0022-0022-0022-000000000001"), null, null, null, "{\"invoiceNumber\":\"100198\",\"reason\":\"La mercancía fue vendida a Comercial Austral SpA antes del retiro; la factura debe emitirse a su nombre.\"}", false, null, null, null, null, null, null, "IMPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "{\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"REINVOICING\",\"requiresPayment\":true,\"amount\":47800,\"taxAmount\":4750,\"totalAmount\":52550,\"currency\":\"CLP\",\"taxRate\":19,\"quantity\":1,\"tierUnit\":null,\"measuredUnits\":null,\"timing\":\"NotApplicable\",\"milestoneAt\":null,\"milestoneSource\":null,\"tariffId\":\"c7f2abc0-29ae-1d0b-d957-ea4707a567b0\",\"tariffCode\":null,\"tariffSource\":\"PORTAL\",\"isExempt\":false,\"exemptionReference\":null,\"excludedContainers\":[],\"lines\":[{\"containerNumber\":null,\"containerType\":null,\"amount\":25000,\"tariffCode\":null,\"breakdown\":[]},{\"containerNumber\":null,\"containerType\":null,\"amount\":22800,\"tariffCode\":\"VAT_LOSS\",\"breakdown\":[]}],\"sourceCharges\":[],\"timeZone\":\"America/Santiago\",\"quotedAt\":\"2026-10-05T16:00:00Z\"}", 1, new DateTime(2026, 10, 5, 16, 0, 0, 0, DateTimeKind.Utc), null, "SRV-20261005-5E1A0009", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, "PendingPayment", new DateTime(2026, 10, 5, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 16, 0, 0, 0, DateTimeKind.Utc), null, new Guid("c7f2abc0-29ae-1d0b-d957-ea4707a567b0"), "PORTAL", 4750m, null, 4, "NotApplicable", 52550m });

            migrationBuilder.InsertData(
                table: "ShipmentDocuments",
                columns: new[] { "Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode" },
                values: new object[] { new Guid("ffffffff-0018-0018-0018-000000000005"), null, new Guid("11111111-0007-0007-0007-000000000018"), "HLCUSAI260701810", "HLCUBKG2607181", "HLXU3071801", null, "application/pdf", "CL", new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "RGO-20261001-1A2B3C4D", "GateOutAdvanceReceipt", "recibo-anticipo-gate-out-RGO-20261001-1A2B3C4D.pdf", "GateOutAdvanceReceipt:0745ba3a-34ac-c833-5db6-bf116971a424", new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000030"), null, null, null, "Seed", new Guid("0745ba3a-34ac-c833-5db6-bf116971a424"), new Guid("55555555-000b-000b-000b-000000000017"), null, new DateTime(2036, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, 0L, "Issued", null, "{\"title\":\"Recibo de pago anticipado - Gate Out\",\"subtitle\":\"Pago recibido antes de la emisi\\u00F3n de la factura\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"RGO-20261001-1A2B3C4D\",\"issuedAt\":\"2026-10-01T14:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260701810\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2607181\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Exportaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Valparaiso Express / 2610N\"},{\"label\":\"Ruta\",\"value\":\"San Antonio (CLSAI) - Callao (PECLL)\"}],\"sections\":[{\"heading\":\"Pagador\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"},{\"label\":\"RUT / NIT\",\"value\":\"96555444-3\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Pago\",\"fields\":[{\"label\":\"Concepto\",\"value\":\"Gate Out - 40HC (San Antonio)\"},{\"label\":\"Monto del cargo\",\"value\":\"71.400,00 CLP\"},{\"label\":\"Monto pagado\",\"value\":\"71.400,00 CLP\"},{\"label\":\"Tipo de cambio\",\"value\":null},{\"label\":\"RUT de facturaci\\u00F3n\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda (96555444-3)\"},{\"label\":\"Pago\",\"value\":\"PAY-20261001-D4E5F6A7\"},{\"label\":\"Comprobante de pago\",\"value\":\"RCP-20261001-E8F9A0B1\"},{\"label\":\"Medio de pago\",\"value\":\"KHIPU\"},{\"label\":\"Fecha de pago\",\"value\":\"01-10-2026 11:00 (America/Santiago)\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3071801\",\"40HC\",\"SL-071801\",\"24.800,00\",\"OnBoard\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Vinculaci\\u00F3n con la factura\",\"fields\":null,\"table\":null,\"paragraphs\":[\"La factura del Gate Out se emite despu\\u00E9s del zarpe de la nave y se vincula a este recibo.\",\"El cargo pagado con este recibo no vuelve a cobrarse al cliente.\"]}],\"verificationCode\":\"CBA5-6DA7-95A4-8C6B\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}", null, null, null, "CBA5-6DA7-95A4-8C6B" });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[,]
                {
                    { new Guid("1b202914-40c5-8096-4927-353dddf8200a"), new Guid("11111111-0007-0007-0007-000000000019"), new Guid("c3d4e5f6-0003-0003-0003-000000000060"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" },
                    { new Guid("5055f8f3-4141-5a05-5c15-fa5d8e322379"), new Guid("11111111-0007-0007-0007-000000000018"), new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "CustomsAgency", "Seed" },
                    { new Guid("7d6585a0-7050-e8d6-4d34-3c0389d1eef6"), new Guid("11111111-0007-0007-0007-000000000018"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Shipper", "Seed" }
                });

            migrationBuilder.InsertData(
                table: "InvoiceReissues",
                columns: new[] { "Id", "AcceptanceExpiresAt", "AcceptanceRequestedAt", "AcceptanceStatus", "AcceptanceTokenHash", "AcceptedAt", "AcceptedByName", "AcceptedByTaxId", "AcceptedFromAddress", "AcceptorEmail", "Currency", "DeclineReason", "DeclinedAt", "ExchangeRate", "FeeAmount", "FeeTaxAmount", "IssuedAt", "NewInvoiceId", "OriginalInvoiceId", "OriginalLegalName", "OriginalSiiNumber", "OriginalSourceNumber", "OriginalTaxId", "ServiceRequestId", "VatLossAmount" },
                values: new object[] { new Guid("ffffffff-0023-0023-0023-000000000001"), new DateTime(2026, 10, 31, 3, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 16, 0, 0, 0, DateTimeKind.Utc), "Pending", "3b238dd0c85c32dc19e44d7fe5588e575b17b19b569c99505e23329dafdb2bfe", null, null, null, null, "facturacion@comercialaustral.cl", "CLP", null, null, null, 25000m, 4750m, null, null, new Guid("ffffffff-0017-0017-0017-000000000002"), "Importadora Demo SpA", "100198", "HL-CL-2026-003987", "76123456-7", new Guid("ffffffff-0021-0021-0021-000000000009"), 22800m });

            migrationBuilder.InsertData(
                table: "PaymentDetails",
                columns: new[] { "Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount" },
                values: new object[,]
                {
                    { new Guid("0745ba3a-34ac-c833-5db6-bf116971a424"), null, 60000m, new Guid("11111111-0007-0007-0007-000000000018"), "Agencia Marítima del Pacífico Ltda", "96555444-3", "HLCUSAI260701810", "HLCUBKG2607181", "GATE_OUT", "CLP", "Gate Out - 40HC (San Antonio)", null, "LocalCharge", null, 71400m, "CLP", new Guid("55555555-000b-000b-000b-000000000017"), new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-0009-0009-0009-000000000034"), 11400m },
                    { new Guid("aab31ddd-35ff-069a-d97b-e746504c1cf4"), null, 25000m, new Guid("11111111-0007-0007-0007-000000000019"), "Distribuidora Andes Crédito SpA", "76000002-2", "HLCUVAP260601930", "HLCUBKG2606193", "ISPS", "CLP", "ISPS", null, "LocalCharge", null, 29750m, "CLP", new Guid("55555555-000b-000b-000b-000000000016"), new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), new Guid("33333333-0009-0009-0009-000000000031"), 4750m },
                    { new Guid("db2bf9c7-ceb5-f5a2-e768-8501f7664a50"), null, 45000m, new Guid("11111111-0007-0007-0007-000000000019"), "Distribuidora Andes Crédito SpA", "76000002-2", "HLCUVAP260601930", "HLCUBKG2606193", "BL_FEE", "CLP", "BL Documentation Fee (import)", null, "LocalCharge", null, 53550m, "CLP", new Guid("55555555-000b-000b-000b-000000000015"), new DateTime(2026, 10, 2, 13, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-0009-0009-0009-000000000030"), 8550m }
                });

            migrationBuilder.InsertData(
                table: "PaymentStatusChanges",
                columns: new[] { "Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus" },
                values: new object[,]
                {
                    { new Guid("0e2ffe61-7b7a-16d8-e1f5-2abef222143b"), new DateTime(2026, 10, 2, 12, 58, 0, 0, DateTimeKind.Utc), "SYSTEM", null, "Pending", new Guid("55555555-000b-000b-000b-000000000015"), "Initiated in BancoChile", "Processing" },
                    { new Guid("1af9cc34-5b6c-90f4-9ca9-19f7778bdc57"), new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), "credito@distribuidoraandes.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000060"), "Pending", new Guid("55555555-000b-000b-000b-000000000016"), "Imputed to the credit line (M5-10)", "Confirmed" },
                    { new Guid("3104f716-436d-9983-05a7-5c405ba0da9c"), new DateTime(2026, 10, 2, 13, 0, 0, 0, DateTimeKind.Utc), "BANCOCHILE_WEBHOOK", null, "Processing", new Guid("55555555-000b-000b-000b-000000000015"), null, "Confirmed" },
                    { new Guid("37ca6d78-0244-c1a5-6957-90caa071b341"), new DateTime(2026, 10, 1, 13, 57, 0, 0, DateTimeKind.Utc), "agente@maritimpacifico.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000030"), null, new Guid("55555555-000b-000b-000b-000000000017"), null, "Pending" },
                    { new Guid("50ab39f7-389c-6890-6be1-b75d01f3f8db"), new DateTime(2026, 10, 3, 10, 30, 0, 0, DateTimeKind.Utc), "credito@distribuidoraandes.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000060"), null, new Guid("55555555-000b-000b-000b-000000000016"), null, "Pending" },
                    { new Guid("9528ce39-c13b-df80-2b2d-4c89e9bed794"), new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), "KHIPU_WEBHOOK", null, "Processing", new Guid("55555555-000b-000b-000b-000000000017"), null, "Confirmed" },
                    { new Guid("a6a9cbb7-07b5-921b-cd4e-2115cf99b383"), new DateTime(2026, 10, 1, 13, 57, 0, 0, DateTimeKind.Utc), "SYSTEM", null, "Pending", new Guid("55555555-000b-000b-000b-000000000017"), "Initiated in Khipu", "Processing" },
                    { new Guid("a6d6c0cc-1fac-a63b-8645-66bc1026ac62"), new DateTime(2026, 10, 2, 12, 58, 0, 0, DateTimeKind.Utc), "credito@distribuidoraandes.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000060"), null, new Guid("55555555-000b-000b-000b-000000000015"), null, "Pending" }
                });

            migrationBuilder.InsertData(
                table: "ServiceRequestAttachments",
                columns: new[] { "Id", "ContentType", "FieldKey", "FileName", "ServiceRequestId", "SizeBytes", "StorageKey", "UploadedAt", "UploadedBy", "UploadedByUserId" },
                values: new object[] { new Guid("754b238a-70ca-d8d1-1d67-0d0da9be0fb5"), "application/pdf", "newCompanyApproval", "aprobacion-comercial-austral.pdf", new Guid("ffffffff-0021-0021-0021-000000000009"), 48213L, "seed/service-requests/aprobacion-comercial-austral.pdf", new DateTime(2026, 10, 5, 15, 50, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010") });

            migrationBuilder.InsertData(
                table: "ServiceRequestCharges",
                columns: new[] { "Id", "Generated", "LocalChargeId", "ServiceRequestId" },
                values: new object[,]
                {
                    { new Guid("4809a14d-d8fc-9a8a-b0b6-14a1930f0b38"), true, new Guid("33333333-0009-0009-0009-000000000036"), new Guid("ffffffff-0021-0021-0021-000000000009") },
                    { new Guid("d4adb761-1298-f523-3999-af07756bdf9c"), true, new Guid("33333333-0009-0009-0009-000000000035"), new Guid("ffffffff-0021-0021-0021-000000000009") }
                });

            migrationBuilder.InsertData(
                table: "ServiceRequestEvents",
                columns: new[] { "Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus" },
                values: new object[,]
                {
                    { new Guid("2037c661-6f62-04e0-5e7a-cc8bc21a8e33"), "System", "SYSTEM", null, "Submitted", "Total 52550 CLP. La factura se emite con el pago y la aceptación de la nueva razón social.", new DateTime(2026, 10, 5, 16, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000009"), "PendingPayment" },
                    { new Guid("837acaa2-8b35-7e99-4adf-08cb72018d26"), "System", "SYSTEM", null, "PendingPayment", "Enlace de aceptación enviado a facturacion@comercialaustral.cl.", new DateTime(2026, 10, 5, 16, 0, 0, 0, DateTimeKind.Utc), 4, new Guid("ffffffff-0021-0021-0021-000000000009"), "PendingPayment" },
                    { new Guid("85d9e28f-8b9f-a6ab-6658-bf2b79378585"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Draft", null, new DateTime(2026, 10, 5, 16, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000009"), "Submitted" },
                    { new Guid("8bf5b0b1-b37c-7ad0-ae50-f7427180845c"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, "Refacturación de la factura 100198.", new DateTime(2026, 10, 5, 15, 40, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000009"), "Draft" }
                });

            migrationBuilder.InsertData(
                table: "ShipmentDocumentEvents",
                columns: new[] { "Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId" },
                values: new object[] { new Guid("c016e13b-8056-1abc-4c0a-b0294e037fbd"), "System", null, "Issued", new DateTime(2026, 10, 1, 14, 0, 0, 0, DateTimeKind.Utc), null, new Guid("c3d4e5f6-0003-0003-0003-000000000030"), null, new Guid("ffffffff-0018-0018-0018-000000000005"), null, null });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_SupersededByInvoiceId",
                table: "CustomerInvoices",
                column: "SupersededByInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeSettlements_BillOfLadingId_ConceptCode_Status",
                table: "ChargeSettlements",
                columns: new[] { "BillOfLadingId", "ConceptCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeSettlements_ItemType_SourceId",
                table: "ChargeSettlements",
                columns: new[] { "ItemType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeSettlements_MatchedInvoiceId",
                table: "ChargeSettlements",
                column: "MatchedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeSettlements_PayerOrganizationId",
                table: "ChargeSettlements",
                column: "PayerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeSettlements_PaymentDetailId",
                table: "ChargeSettlements",
                column: "PaymentDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditImputationRules_Country_ConceptCode",
                table: "CreditImputationRules",
                columns: new[] { "Country", "ConceptCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DepositProofs_PaymentId_UploadedAt",
                table: "DepositProofs",
                columns: new[] { "PaymentId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DepositProofs_Status_UploadedAt",
                table: "DepositProofs",
                columns: new[] { "Status", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReissues_AcceptanceTokenHash",
                table: "InvoiceReissues",
                column: "AcceptanceTokenHash",
                unique: true,
                filter: "\"AcceptanceTokenHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReissues_OriginalInvoiceId",
                table: "InvoiceReissues",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReissues_ServiceRequestId",
                table: "InvoiceReissues",
                column: "ServiceRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargeSettlements");

            migrationBuilder.DropTable(
                name: "CreditImputationRules");

            migrationBuilder.DropTable(
                name: "DepositProofs");

            migrationBuilder.DropTable(
                name: "InvoiceReissues");

            migrationBuilder.DropIndex(
                name: "IX_CustomerInvoices_SupersededByInvoiceId",
                table: "CustomerInvoices");

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000023"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000024"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("51c3d02a-634f-2135-64c7-0c59a15165fb"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("923fac6f-9222-dbf9-90ea-4f267bafce41"));

            migrationBuilder.DeleteData(
                table: "ConfigurationSettings",
                keyColumn: "Id",
                keyValue: new Guid("11f1dde1-1820-e7fe-1e07-4aa557ecd21c"));

            migrationBuilder.DeleteData(
                table: "ConfigurationSettings",
                keyColumn: "Id",
                keyValue: new Guid("faff8f22-0d64-9389-8e15-d248dd2301b5"));

            migrationBuilder.DeleteData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000011"));

            migrationBuilder.DeleteData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000012"));

            migrationBuilder.DeleteData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000013"));

            migrationBuilder.DeleteData(
                table: "CustomerInvoices",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0017-0017-0017-000000000014"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000030"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000031"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000032"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000033"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000034"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000035"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000036"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("218d2183-e252-8852-de39-cd0104d6a085"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("2d9f6544-3c95-8cf5-f6dd-e808cedd59ce"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("89c5b237-f1a2-5a8e-335a-e2855cd4e5bb"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("afa9137e-bf2b-5690-fe33-0cb261464598"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("b7fe99ef-e1c5-2eba-49b5-12812d0cc83a"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("e09a7df1-2e24-226b-f05d-131b4282f10b"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("0745ba3a-34ac-c833-5db6-bf116971a424"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("aab31ddd-35ff-069a-d97b-e746504c1cf4"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("db2bf9c7-ceb5-f5a2-e768-8501f7664a50"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("0e2ffe61-7b7a-16d8-e1f5-2abef222143b"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("1af9cc34-5b6c-90f4-9ca9-19f7778bdc57"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("3104f716-436d-9983-05a7-5c405ba0da9c"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("37ca6d78-0244-c1a5-6957-90caa071b341"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("50ab39f7-389c-6890-6be1-b75d01f3f8db"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("9528ce39-c13b-df80-2b2d-4c89e9bed794"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("a6a9cbb7-07b5-921b-cd4e-2115cf99b383"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("a6d6c0cc-1fac-a63b-8645-66bc1026ac62"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestAttachments",
                keyColumn: "Id",
                keyValue: new Guid("754b238a-70ca-d8d1-1d67-0d0da9be0fb5"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestCharges",
                keyColumn: "Id",
                keyValue: new Guid("4809a14d-d8fc-9a8a-b0b6-14a1930f0b38"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestCharges",
                keyColumn: "Id",
                keyValue: new Guid("d4adb761-1298-f523-3999-af07756bdf9c"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("2037c661-6f62-04e0-5e7a-cc8bc21a8e33"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("837acaa2-8b35-7e99-4adf-08cb72018d26"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("85d9e28f-8b9f-a6ab-6658-bf2b79378585"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("8bf5b0b1-b37c-7ad0-ae50-f7427180845c"));

            migrationBuilder.DeleteData(
                table: "ShipmentDocumentEvents",
                keyColumn: "Id",
                keyValue: new Guid("c016e13b-8056-1abc-4c0a-b0294e037fbd"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("1b202914-40c5-8096-4927-353dddf8200a"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("5055f8f3-4141-5a05-5c15-fa5d8e322379"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("7d6585a0-7050-e8d6-4d34-3c0389d1eef6"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("c7f2abc0-29ae-1d0b-d957-ea4707a567b0"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000015"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000016"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000017"));

            migrationBuilder.DeleteData(
                table: "ServiceRequests",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0021-0021-0021-000000000009"));

            migrationBuilder.DeleteData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000005"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000018"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000019"));

            migrationBuilder.DeleteData(
                table: "ServiceDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0022-0022-0022-000000000001"));

            migrationBuilder.DropColumn(
                name: "SupersededByInvoiceId",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "SupersedesInvoiceId",
                table: "CustomerInvoices");
        }
    }
}
