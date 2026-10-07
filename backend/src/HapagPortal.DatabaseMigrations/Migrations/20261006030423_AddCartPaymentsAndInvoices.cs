using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCartPaymentsAndInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ClientId",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "BillOfLadingId",
                table: "Payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "Payments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByRole",
                table: "Payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Los pagos existentes son anteriores al carro: quedan como "Legacy".
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<string>(
                name: "PayerName",
                table: "Payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayerTaxId",
                table: "Payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodCode",
                table: "Payments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderKey",
                table: "Payments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderReference",
                table: "Payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransactionId",
                table: "Payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedirectUrl",
                table: "Payments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFingerprint",
                table: "Payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlipIssuedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlipNumber",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccessGrantId",
                table: "PaymentDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BillOfLadingId",
                table: "PaymentDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingName",
                table: "PaymentDetails",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingTaxId",
                table: "PaymentDetails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlNumber",
                table: "PaymentDetails",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingNumber",
                table: "PaymentDetails",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PaymentDetails",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "PaymentDetails",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OnBehalfOfClientId",
                table: "PaymentDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "PaymentDetails",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalCurrency",
                table: "PaymentDetails",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                table: "PaymentDetails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "PaymentDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FreightPaidAt",
                table: "BillsOfLading",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiiNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SourceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BookingNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SiiStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IsPayable = table.Column<bool>(type: "boolean", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerInvoices_Clients_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentBlockWindows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ClientMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentBlockWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentCurrencyRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCurrencyRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethodConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Currencies = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentOutboxMessages_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentStatusChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentStatusChanges_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BookingNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    PaymentCurrency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    RateEffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RateSource = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    BillingTaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BillingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BillingOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OnBehalfOfClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessGrantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedByPaymentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000001"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000002"),
                column: "FreightPaidAt",
                value: new DateTime(2026, 9, 20, 14, 5, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000003"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000004"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000005"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000006"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000007"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000008"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000009"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000010"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000011"),
                column: "FreightPaidAt",
                value: null);

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ExchangeRateToUSD", "LastUpdated", "ModifiedAt", "ModifiedBy", "Name", "Symbol" },
                values: new object[] { new Guid("a1b2c3d4-0001-0001-0001-000000000004"), "EUR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 0.92m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Euro", "€" });

            migrationBuilder.InsertData(
                table: "CustomerInvoices",
                columns: new[] { "Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0017-0017-0017-000000000001"), new Guid("11111111-0007-0007-0007-000000000009"), "HLCUSAI260400910", "HLCUBKG2604091", "DEMURRAGE", "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "ExemptInvoice", new DateOnly(2026, 10, 15), true, new DateOnly(2026, 9, 15), "Importadora Demo SpA", null, null, 510000m, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "100245", "ACCEPTED", "DUMMY", "FAC-DEM-2026-0915", "Pending", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 0m, "76123456-7", 510000m },
                    { new Guid("ffffffff-0017-0017-0017-000000000002"), new Guid("11111111-0007-0007-0007-000000000001"), "HLCUVAL250100123", "HLCUBKG2501001", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Invoice", new DateOnly(2026, 9, 19), true, new DateOnly(2026, 8, 20), "Importadora Demo SpA", null, null, 120000m, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "100198", "ACCEPTED", "DUMMY", "HL-CL-2026-003987", "Pending", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 22800m, "76123456-7", 142800m },
                    { new Guid("ffffffff-0017-0017-0017-000000000003"), new Guid("11111111-0007-0007-0007-000000000002"), "HLCUVAL250200456", "HLCUBKG2502004", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Invoice", new DateOnly(2026, 8, 9), false, new DateOnly(2026, 7, 10), "Importadora Demo SpA", null, null, 185000m, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 8, 5, 15, 0, 0, 0, DateTimeKind.Utc), null, "100150", "ACCEPTED", "DUMMY", "HL-CL-2026-003501", "Paid", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 35150m, "76123456-7", 220150m },
                    { new Guid("ffffffff-0017-0017-0017-000000000004"), new Guid("11111111-0007-0007-0007-000000000006"), "HLCUSAI260300610", "HLCUBKG2603061", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "ExemptInvoice", new DateOnly(2026, 11, 1), true, new DateOnly(2026, 10, 2), "Importadora Demo SpA", null, null, 350m, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, null, null, "DUMMY", "HL-CL-2026-004601", "Pending", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 0m, "76123456-7", 350m },
                    { new Guid("ffffffff-0017-0017-0017-000000000005"), new Guid("11111111-0007-0007-0007-000000000001"), "HLCUVAL250100123", "HLCUBKG2501001", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "CreditNote", null, false, new DateOnly(2026, 9, 25), "Importadora Demo SpA", null, null, 15000m, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "100301", "ACCEPTED", "DUMMY", "HL-CL-2026-004700", "Paid", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 2850m, "76123456-7", 17850m },
                    { new Guid("ffffffff-0017-0017-0017-000000000006"), new Guid("11111111-0007-0007-0007-000000000010"), "HLCUSAI260401020", "HLCUBKG2604102", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "EUR", null, null, "ExemptInvoice", new DateOnly(2026, 10, 28), true, new DateOnly(2026, 9, 28), "Importadora Demo SpA", null, null, 480m, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "100260", "ACCEPTED", "DUMMY", "HL-CL-2026-004530", "Pending", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 0m, "76123456-7", 480m },
                    { new Guid("ffffffff-0017-0017-0017-000000000007"), new Guid("11111111-0007-0007-0007-000000000004"), "HLCUARI260100045", "HLCUBKG2601045", null, "BO", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Invoice", new DateOnly(2026, 10, 10), true, new DateOnly(2026, 9, 10), "Comercial Altiplano SRL", null, null, 1280m, new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, "2026-000812", "ACCEPTED", "DUMMY", "HL-BO-2026-000812", "Pending", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 166.40m, "1023456017", 1446.40m },
                    { new Guid("ffffffff-0017-0017-0017-000000000008"), new Guid("11111111-0007-0007-0007-000000000005"), "HLCUIQQ260200078", "HLCUBKG2602078", null, "BO", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Invoice", new DateOnly(2026, 9, 29), false, new DateOnly(2026, 8, 30), "Comercial Altiplano SRL", null, null, 690m, new Guid("c3d4e5f6-0003-0003-0003-000000000020"), new DateTime(2026, 9, 5, 14, 0, 0, 0, DateTimeKind.Utc), null, "2026-000790", "ACCEPTED", "DUMMY", "HL-BO-2026-000790", "Paid", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 89.70m, "1023456017", 779.70m },
                    { new Guid("ffffffff-0017-0017-0017-000000000009"), new Guid("11111111-0007-0007-0007-000000000011"), "HLCUVAP260401130", "HLCUBKG2604113", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Invoice", new DateOnly(2026, 10, 26), true, new DateOnly(2026, 9, 26), "Distribuidora Andes Crédito SpA", null, null, 95000m, new Guid("c3d4e5f6-0003-0003-0003-000000000060"), null, null, "100277", "ACCEPTED", "DUMMY", "HL-CL-2026-004588", "Pending", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 18050m, "76000002-2", 113050m },
                    { new Guid("ffffffff-0017-0017-0017-000000000010"), new Guid("11111111-0007-0007-0007-000000000011"), "HLCUVAP260401130", "HLCUBKG2604113", null, "CL", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "ExemptInvoice", new DateOnly(2026, 10, 26), true, new DateOnly(2026, 9, 26), "Distribuidora Andes Crédito SpA", null, null, 380m, new Guid("c3d4e5f6-0003-0003-0003-000000000060"), null, null, "100278", "ACCEPTED", "DUMMY", "HL-CL-2026-004589", "Pending", new DateTime(2026, 10, 5, 11, 0, 0, 0, DateTimeKind.Utc), 0m, "76000002-2", 380m }
                });

            migrationBuilder.InsertData(
                table: "ExchangeRateRecords",
                columns: new[] { "Id", "Approved", "CapturedAt", "ConvertedAmount", "EffectiveDate", "FromCurrency", "Rate", "Source", "SourceAmount", "ToCurrency", "TransactionId", "TransactionType" },
                values: new object[] { new Guid("b3a141c8-4669-b188-6d5a-edaf7cacd5a5"), true, new DateTime(2026, 9, 20, 14, 2, 0, 0, DateTimeKind.Utc), 4940000m, new DateOnly(2026, 9, 20), "USD", 950m, "DUMMY", 5200m, "CLP", new Guid("55555555-000b-000b-000b-000000000010"), "Payment" });

            migrationBuilder.InsertData(
                table: "MaintainerChangeLogs",
                columns: new[] { "Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue" },
                values: new object[,]
                {
                    { new Guid("009d0f99-085a-ba06-8922-5a0493f16195"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("4ae9a380-c47c-76a4-0462-742d9ebd1d52"), "PaymentCurrency", "{\"country\":\"BO\",\"conceptCode\":\"EDS\",\"currency\":\"BOB\",\"isEnabled\":true}", null },
                    { new Guid("21fe52a2-53f3-ea6f-16e7-786a3d0e0356"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("9d493c1f-625d-2ddd-c5cd-d2f6ced94174"), "PaymentCurrency", "{\"country\":\"BO\",\"conceptCode\":\"ADVANCE_DEMURRAGE_BO\",\"currency\":\"USD\",\"isEnabled\":true}", null },
                    { new Guid("25c18b0f-18ba-3150-1be7-88545cabf81d"), "Created", new DateTime(2026, 9, 26, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0016-0016-0016-000000000001"), "PaymentBlockWindow", "{\"country\":\"CL\",\"startDate\":\"2026-09-30\",\"startTime\":\"20:00:00\",\"endDate\":\"2026-09-30\",\"endTime\":\"23:59:00\",\"reason\":\"Cierre contable de septiembre\",\"clientMessage\":\"Los pagos est\\u00E1n suspendidos temporalmente por el cierre contable mensual. Podr\\u00E1 pagar nuevamente desde las 23:59 (hora de Chile).\",\"isActive\":true}", null },
                    { new Guid("2cf55acb-0012-8fc4-a30b-b8e6b5dfba2f"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0016-0016-0016-000000000003"), "PaymentBlockWindow", "{\"country\":\"BO\",\"startDate\":\"2026-12-24\",\"startTime\":\"18:00:00\",\"endDate\":\"2026-12-26\",\"endTime\":\"08:00:00\",\"reason\":\"Mantenimiento de la conciliaci\\u00F3n bancaria de fin de a\\u00F1o\",\"clientMessage\":\"Los pagos en l\\u00EDnea no est\\u00E1n disponibles por mantenimiento hasta el 26-12 a las 08:00 (hora de Bolivia).\",\"isActive\":true}", null },
                    { new Guid("301dfc03-3a4c-659e-f88b-048be09d4b13"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("70274a14-0ca5-1374-71dd-1a757cbcda98"), "PaymentCurrency", "{\"country\":\"CL\",\"conceptCode\":\"FREIGHT\",\"currency\":\"USD\",\"isEnabled\":true}", null },
                    { new Guid("3153499e-1612-eefd-7af8-7804b5d8f54d"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("95ce57ff-9e48-2759-c9c9-b25672f9ac6b"), "PaymentMethod", "{\"code\":\"BANK_BUTTON_BCI\",\"name\":\"Bot\\u00F3n de pago Bci\",\"description\":\"Pago en l\\u00EDnea desde la banca de Bci\",\"country\":\"CL\",\"kind\":\"Online\",\"providerKey\":\"Bci\",\"currencies\":[\"CLP\"],\"isEnabled\":true,\"displayOrder\":40}", null },
                    { new Guid("3b8aa0fa-17d0-516c-5957-2018b8e21757"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("deea0f57-7ac8-473d-1b7f-821284393535"), "PaymentMethod", "{\"code\":\"DIGITAL_USD\",\"name\":\"D\\u00F3lares digitales\",\"description\":\"Reservado (M5-03): se habilita cuando se defina el proveedor\",\"country\":\"BO\",\"kind\":\"Online\",\"providerKey\":null,\"currencies\":[\"USD\"],\"isEnabled\":false,\"displayOrder\":90}", null },
                    { new Guid("48c2038f-b24f-3b7f-1210-08fc6d7c45df"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("d72f12ae-5666-dd92-37dc-6284173fa6ee"), "PaymentMethod", "{\"code\":\"DIGITAL_USD\",\"name\":\"D\\u00F3lares digitales\",\"description\":\"Reservado (M5-03): se habilita cuando se defina el proveedor\",\"country\":\"CL\",\"kind\":\"Online\",\"providerKey\":null,\"currencies\":[\"USD\"],\"isEnabled\":false,\"displayOrder\":90}", null },
                    { new Guid("5f20b6bf-6258-7dfc-9337-83786ed0c2e6"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("cc949890-ea1d-885b-57ab-3067028e5c54"), "PaymentCurrency", "{\"country\":\"BO\",\"conceptCode\":\"DEMURRAGE\",\"currency\":\"BOB\",\"isEnabled\":true}", null },
                    { new Guid("67823a49-3a13-1a79-15f6-097407733cc1"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("a886a06b-0336-a30f-36c1-dff222504ce2"), "PaymentMethod", "{\"code\":\"DEPOSIT\",\"name\":\"Dep\\u00F3sito bancario (boleta)\",\"description\":\"Boleta para dep\\u00F3sito o transferencia; Finanzas confirma el abono\",\"country\":\"CL\",\"kind\":\"Deposit\",\"providerKey\":null,\"currencies\":[\"CLP\",\"USD\",\"EUR\"],\"isEnabled\":true,\"displayOrder\":50}", null },
                    { new Guid("6e624596-577b-cfa9-7f30-a43d2de5f5cd"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("86aec5dc-4971-4b36-8482-52ce8c3f8732"), "PaymentMethod", "{\"code\":\"BANK_BUTTON_SANTANDER\",\"name\":\"Bot\\u00F3n de pago Santander\",\"description\":\"Pago en l\\u00EDnea desde la banca de Santander\",\"country\":\"CL\",\"kind\":\"Online\",\"providerKey\":\"Santander\",\"currencies\":[\"CLP\"],\"isEnabled\":true,\"displayOrder\":30}", null },
                    { new Guid("6ee41684-f5b7-0176-23ed-ab6b94f0815f"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("9022d3e1-11fc-7493-a0c4-de6760961bb3"), "PaymentMethod", "{\"code\":\"BANK_BUTTON_BCH\",\"name\":\"Bot\\u00F3n de pago Banco de Chile\",\"description\":\"Pago en l\\u00EDnea desde la banca del Banco de Chile\",\"country\":\"CL\",\"kind\":\"Online\",\"providerKey\":\"BancoChile\",\"currencies\":[\"CLP\",\"USD\"],\"isEnabled\":true,\"displayOrder\":20}", null },
                    { new Guid("7126d08c-77ab-5a25-a793-ea20c39fbf0e"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("a6cb3763-7416-78bc-99f2-75cf1e99d2ec"), "PaymentCurrency", "{\"country\":\"CL\",\"conceptCode\":\"DEMURRAGE\",\"currency\":\"USD\",\"isEnabled\":true}", null },
                    { new Guid("8806e939-b593-2530-fda8-1e06219d03c3"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("dce8145f-b0ce-739e-c803-950f92f73222"), "PaymentMethod", "{\"code\":\"DEPOSIT\",\"name\":\"Dep\\u00F3sito o transferencia bancaria (boleta)\",\"description\":\"Boleta para dep\\u00F3sito o transferencia; Finanzas confirma el abono\",\"country\":\"BO\",\"kind\":\"Deposit\",\"providerKey\":null,\"currencies\":[\"BOB\",\"USD\"],\"isEnabled\":true,\"displayOrder\":10}", null },
                    { new Guid("9516cb88-c63f-aee3-2979-8ac69d5688c9"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("96eed327-ce91-3e86-e713-0bdcbd30c481"), "PaymentMethod", "{\"code\":\"KHIPU\",\"name\":\"Khipu\",\"description\":\"Transferencia simplificada con Khipu\",\"country\":\"CL\",\"kind\":\"Online\",\"providerKey\":\"Khipu\",\"currencies\":[\"CLP\"],\"isEnabled\":true,\"displayOrder\":10}", null },
                    { new Guid("963f22f1-c90b-4145-9d68-d23b271dbef5"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("39797e93-73d2-b3ca-5043-aa9df7cf517d"), "PaymentCurrency", "{\"country\":\"CL\",\"conceptCode\":\"DEMURRAGE\",\"currency\":\"CLP\",\"isEnabled\":true}", null },
                    { new Guid("9707c44c-468c-fba3-bac1-d2b3e24d55a6"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("c2a0e616-edb8-7916-784c-9a2d7a9acb8d"), "PaymentCurrency", "{\"country\":\"CL\",\"conceptCode\":\"GATE_IN\",\"currency\":\"CLP\",\"isEnabled\":true}", null },
                    { new Guid("99e1ee9a-4cb8-6a8f-7834-61b67cca8c41"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("323b3d52-1913-dfa7-b527-56610206d402"), "PaymentCurrency", "{\"country\":\"BO\",\"conceptCode\":\"GATE_IN\",\"currency\":\"BOB\",\"isEnabled\":true}", null },
                    { new Guid("a006e2af-63e5-00bb-7f00-8741b4d8b17e"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("4398a676-9729-32fe-fd7a-8864d3f969ae"), "PaymentCurrency", "{\"country\":\"BO\",\"conceptCode\":\"FREIGHT\",\"currency\":\"BOB\",\"isEnabled\":true}", null },
                    { new Guid("a0d3fb65-c917-d6fe-11d7-97d7ab5fa000"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("89707cfa-2d11-e597-117a-6925557cdd58"), "PaymentCurrency", "{\"country\":\"BO\",\"conceptCode\":\"ADVANCE_DEMURRAGE_BO\",\"currency\":\"BOB\",\"isEnabled\":true}", null },
                    { new Guid("a817ee90-0e4b-823a-f85e-86d26390a1f6"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("b83dc854-2b7a-489f-855c-ce679da9658b"), "PaymentCurrency", "{\"country\":\"BO\",\"conceptCode\":\"FREIGHT\",\"currency\":\"USD\",\"isEnabled\":true}", null },
                    { new Guid("bcafbb5c-b285-5fd1-9598-f72a60f20ca7"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("317d9770-1994-f770-232f-44f9a1f6d995"), "PaymentCurrency", "{\"country\":\"BO\",\"conceptCode\":\"DEMURRAGE\",\"currency\":\"USD\",\"isEnabled\":true}", null },
                    { new Guid("cdeac7b5-d7b7-92d9-a901-a94bdedd39ab"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0016-0016-0016-000000000002"), "PaymentBlockWindow", "{\"country\":null,\"startDate\":\"2026-10-31\",\"startTime\":\"21:00:00\",\"endDate\":\"2026-11-01\",\"endTime\":\"06:00:00\",\"reason\":\"Cierre contable de octubre\",\"clientMessage\":\"Los pagos est\\u00E1n suspendidos temporalmente por el cierre contable mensual. Podr\\u00E1 pagar nuevamente a partir de las 06:00 (hora local).\",\"isActive\":true}", null },
                    { new Guid("d0f6487e-45c1-3e34-5a1d-26a1d0ed6f70"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("20ab80b2-72d5-0892-de39-0ad92903130f"), "PaymentCurrency", "{\"country\":\"CL\",\"conceptCode\":\"EDS\",\"currency\":\"CLP\",\"isEnabled\":true}", null },
                    { new Guid("d271dc82-d532-ec69-794c-0d0d2193e12f"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("0dabc090-4865-6f5e-3d2a-10da65adb6f7"), "PaymentCurrency", "{\"country\":\"CL\",\"conceptCode\":\"FREIGHT\",\"currency\":\"EUR\",\"isEnabled\":true}", null },
                    { new Guid("e0b8fcc8-f790-c0b9-6959-b0c38b58753c"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("7d6dbb49-443c-1eb5-790a-d5e2aa6594b1"), "PaymentCurrency", "{\"country\":\"CL\",\"conceptCode\":\"FREIGHT\",\"currency\":\"CLP\",\"isEnabled\":true}", null }
                });

            migrationBuilder.InsertData(
                table: "PaymentBlockWindows",
                columns: new[] { "Id", "ClientMessage", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "EndDate", "EndTime", "IsActive", "ModifiedAt", "ModifiedBy", "Reason", "StartDate", "StartTime" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0016-0016-0016-000000000001"), "Los pagos están suspendidos temporalmente por el cierre contable mensual. Podrá pagar nuevamente desde las 23:59 (hora de Chile).", "CL", new DateTime(2026, 9, 26, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateOnly(2026, 9, 30), new TimeOnly(23, 59, 0), true, null, null, "Cierre contable de septiembre", new DateOnly(2026, 9, 30), new TimeOnly(20, 0, 0) },
                    { new Guid("ffffffff-0016-0016-0016-000000000002"), "Los pagos están suspendidos temporalmente por el cierre contable mensual. Podrá pagar nuevamente a partir de las 06:00 (hora local).", null, new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateOnly(2026, 11, 1), new TimeOnly(6, 0, 0), true, null, null, "Cierre contable de octubre", new DateOnly(2026, 10, 31), new TimeOnly(21, 0, 0) },
                    { new Guid("ffffffff-0016-0016-0016-000000000003"), "Los pagos en línea no están disponibles por mantenimiento hasta el 26-12 a las 08:00 (hora de Bolivia).", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateOnly(2026, 12, 26), new TimeOnly(8, 0, 0), true, null, null, "Mantenimiento de la conciliación bancaria de fin de año", new DateOnly(2026, 12, 24), new TimeOnly(18, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "PaymentCurrencyRules",
                columns: new[] { "Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy" },
                values: new object[,]
                {
                    { new Guid("0dabc090-4865-6f5e-3d2a-10da65adb6f7"), "FREIGHT", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "EUR", null, null, true, null, null },
                    { new Guid("20ab80b2-72d5-0892-de39-0ad92903130f"), "EDS", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, true, null, null },
                    { new Guid("317d9770-1994-f770-232f-44f9a1f6d995"), "DEMURRAGE", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, true, null, null },
                    { new Guid("323b3d52-1913-dfa7-b527-56610206d402"), "GATE_IN", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, true, null, null },
                    { new Guid("39797e93-73d2-b3ca-5043-aa9df7cf517d"), "DEMURRAGE", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, true, null, null },
                    { new Guid("4398a676-9729-32fe-fd7a-8864d3f969ae"), "FREIGHT", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, true, null, null },
                    { new Guid("4ae9a380-c47c-76a4-0462-742d9ebd1d52"), "EDS", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, true, null, null },
                    { new Guid("70274a14-0ca5-1374-71dd-1a757cbcda98"), "FREIGHT", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, true, null, null },
                    { new Guid("7d6dbb49-443c-1eb5-790a-d5e2aa6594b1"), "FREIGHT", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, true, null, null },
                    { new Guid("89707cfa-2d11-e597-117a-6925557cdd58"), "ADVANCE_DEMURRAGE_BO", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, true, null, null },
                    { new Guid("9d493c1f-625d-2ddd-c5cd-d2f6ced94174"), "ADVANCE_DEMURRAGE_BO", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, true, null, null },
                    { new Guid("a6cb3763-7416-78bc-99f2-75cf1e99d2ec"), "DEMURRAGE", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, true, null, null },
                    { new Guid("b83dc854-2b7a-489f-855c-ce679da9658b"), "FREIGHT", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, true, null, null },
                    { new Guid("c2a0e616-edb8-7916-784c-9a2d7a9acb8d"), "GATE_IN", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, true, null, null },
                    { new Guid("cc949890-ea1d-885b-57ab-3067028e5c54"), "DEMURRAGE", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, true, null, null }
                });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000001"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000002"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000003"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000004"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000005"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000006"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000007"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000008"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000009"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000010"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000011"),
                columns: new[] { "AccessGrantId", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "ReleasedAt", "SourceId" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.InsertData(
                table: "PaymentMethodConfigs",
                columns: new[] { "Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey" },
                values: new object[,]
                {
                    { new Guid("86aec5dc-4971-4b36-8482-52ce8c3f8732"), "BANK_BUTTON_SANTANDER", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Pago en línea desde la banca de Santander", 30, true, "Online", null, null, "Botón de pago Santander", "Santander" },
                    { new Guid("9022d3e1-11fc-7493-a0c4-de6760961bb3"), "BANK_BUTTON_BCH", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP,USD", null, null, "Pago en línea desde la banca del Banco de Chile", 20, true, "Online", null, null, "Botón de pago Banco de Chile", "BancoChile" },
                    { new Guid("95ce57ff-9e48-2759-c9c9-b25672f9ac6b"), "BANK_BUTTON_BCI", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Pago en línea desde la banca de Bci", 40, true, "Online", null, null, "Botón de pago Bci", "Bci" },
                    { new Guid("96eed327-ce91-3e86-e713-0bdcbd30c481"), "KHIPU", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Transferencia simplificada con Khipu", 10, true, "Online", null, null, "Khipu", "Khipu" },
                    { new Guid("a886a06b-0336-a30f-36c1-dff222504ce2"), "DEPOSIT", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP,USD,EUR", null, null, "Boleta para depósito o transferencia; Finanzas confirma el abono", 50, true, "Deposit", null, null, "Depósito bancario (boleta)", null },
                    { new Guid("d72f12ae-5666-dd92-37dc-6284173fa6ee"), "DIGITAL_USD", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Reservado (M5-03): se habilita cuando se defina el proveedor", 90, false, "Online", null, null, "Dólares digitales", null },
                    { new Guid("dce8145f-b0ce-739e-c803-950f92f73222"), "DEPOSIT", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB,USD", null, null, "Boleta para depósito o transferencia; Finanzas confirma el abono", 10, true, "Deposit", null, null, "Depósito o transferencia bancaria (boleta)", null },
                    { new Guid("deea0f57-7ac8-473d-1b7f-821284393535"), "DIGITAL_USD", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Reservado (M5-03): se habilita cuando se defina el proveedor", 90, false, "Online", null, null, "Dólares digitales", null }
                });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000001"),
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "CreatedByUserId", "FailureReason", "IdempotencyKey", "Origin", "PayerName", "PayerTaxId", "PaymentMethodCode", "ProviderKey", "ProviderReference", "ProviderTransactionId", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "StatusChangedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, "Legacy", null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000002"),
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "CreatedByUserId", "FailureReason", "IdempotencyKey", "Origin", "PayerName", "PayerTaxId", "PaymentMethodCode", "ProviderKey", "ProviderReference", "ProviderTransactionId", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "StatusChangedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, "Legacy", null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000003"),
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "CreatedByUserId", "FailureReason", "IdempotencyKey", "Origin", "PayerName", "PayerTaxId", "PaymentMethodCode", "ProviderKey", "ProviderReference", "ProviderTransactionId", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "StatusChangedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, "Legacy", null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000004"),
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "CreatedByUserId", "FailureReason", "IdempotencyKey", "Origin", "PayerName", "PayerTaxId", "PaymentMethodCode", "ProviderKey", "ProviderReference", "ProviderTransactionId", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "StatusChangedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, "Legacy", null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000005"),
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "CreatedByUserId", "FailureReason", "IdempotencyKey", "Origin", "PayerName", "PayerTaxId", "PaymentMethodCode", "ProviderKey", "ProviderReference", "ProviderTransactionId", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "StatusChangedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, "Legacy", null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000006"),
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "CreatedByUserId", "FailureReason", "IdempotencyKey", "Origin", "PayerName", "PayerTaxId", "PaymentMethodCode", "ProviderKey", "ProviderReference", "ProviderTransactionId", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "StatusChangedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, "Legacy", null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000007"),
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "CreatedByUserId", "FailureReason", "IdempotencyKey", "Origin", "PayerName", "PayerTaxId", "PaymentMethodCode", "ProviderKey", "ProviderReference", "ProviderTransactionId", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "StatusChangedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, "Legacy", null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000008"),
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "CreatedByUserId", "FailureReason", "IdempotencyKey", "Origin", "PayerName", "PayerTaxId", "PaymentMethodCode", "ProviderKey", "ProviderReference", "ProviderTransactionId", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "StatusChangedAt" },
                values: new object[] { null, null, null, null, null, null, null, null, "Legacy", null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("55555555-000b-000b-000b-000000000009"), null, 70000m, null, null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 9, 12, 15, 20, 0, 0, DateTimeKind.Utc), "KHIPU_WEBHOOK", "CL", new DateTime(2026, 9, 12, 15, 20, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000010", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "CLP", null, null, null, null, "PAY-20260912-1A2B3C4D", null, null, null, null, null, "Cart", "Importadora Demo SpA", "76123456-7", new DateTime(2026, 9, 12, 15, 20, 0, 0, DateTimeKind.Utc), "KHIPU", "KHIPU", "PAY-20260912-1A2B3C4D", "Cart", "Khipu", "DUMMY-KHIPU-PAY-20260912-1A2B3C4D", "KHP-TXN-8812345", "RCP-20260912-7F3A21C4", null, null, null, null, "Confirmed", new DateTime(2026, 9, 12, 15, 20, 0, 0, DateTimeKind.Utc), 13300m, 83300m },
                    { new Guid("55555555-000b-000b-000b-000000000010"), new Guid("cccccccc-0012-0012-0012-000000000002"), 4940000m, new Guid("11111111-0007-0007-0007-000000000002"), null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new DateTime(2026, 9, 20, 14, 5, 0, 0, DateTimeKind.Utc), "BANCOCHILE_WEBHOOK", "CL", new DateTime(2026, 9, 20, 14, 5, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000030", new Guid("d4e5f6a7-0004-0004-0004-000000000030"), "CLP", null, null, null, 950m, "PAY-20260920-5E6F7A8B", null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Cart", "Agencia Marítima del Pacífico Ltda", "96555444-3", new DateTime(2026, 9, 20, 14, 5, 0, 0, DateTimeKind.Utc), "BANK_BUTTON_BCH", "BANK_BUTTON_BCH", "PAY-20260920-5E6F7A8B", "Cart", "BancoChile", "DUMMY-BANCOCHILE-PAY-20260920-5E6F7A8B", "BCH-TXN-55100231", "RCP-20260920-2B4D6F80", null, null, null, null, "Confirmed", new DateTime(2026, 9, 20, 14, 5, 0, 0, DateTimeKind.Utc), 0m, 4940000m },
                    { new Guid("55555555-000b-000b-000b-000000000011"), null, 45000m, new Guid("11111111-0007-0007-0007-000000000006"), null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "CL", new DateTime(2026, 10, 3, 13, 30, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000010", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "CLP", null, null, null, null, "PAY-20261003-9C8B7A6D", null, null, null, null, null, "Cart", "Importadora Demo SpA", "76123456-7", new DateTime(2026, 10, 3, 13, 30, 0, 0, DateTimeKind.Utc), "DEPOSIT", "DEPOSIT", "PAY-20261003-9C8B7A6D", "Cart", null, null, null, null, null, null, new DateTime(2026, 10, 3, 13, 30, 0, 0, DateTimeKind.Utc), "BDP-20261003-5C7D9E1F", "PendingVerification", new DateTime(2026, 10, 3, 13, 30, 0, 0, DateTimeKind.Utc), 8550m, 53550m },
                    { new Guid("55555555-000b-000b-000b-000000000012"), null, 85000m, new Guid("11111111-0007-0007-0007-000000000009"), null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "CL", new DateTime(2026, 10, 4, 16, 45, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000010", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "CLP", null, null, null, null, "PAY-20261004-3D2C1B0A", "PROVIDER_UNAVAILABLE", null, null, null, null, "Cart", "Importadora Demo SpA", "76123456-7", new DateTime(2026, 10, 4, 16, 45, 0, 0, DateTimeKind.Utc), "KHIPU", "KHIPU", "PAY-20261004-3D2C1B0A", "Cart", "Khipu", null, null, null, null, null, null, null, "Failed", new DateTime(2026, 10, 4, 16, 45, 0, 0, DateTimeKind.Utc), 16150m, 101150m },
                    { new Guid("55555555-000b-000b-000b-000000000013"), null, 820m, null, null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000020"), new DateTime(2026, 9, 5, 14, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", "BO", new DateTime(2026, 9, 5, 14, 0, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000020", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), "BOB", null, null, null, null, "PAY-20260903-4A5B6C7D", null, null, null, null, null, "Cart", "Comercial Altiplano SRL", "1023456017", new DateTime(2026, 9, 5, 14, 0, 0, 0, DateTimeKind.Utc), "DEPOSIT", "DEPOSIT", "PAY-20260903-4A5B6C7D", "Cart", null, null, null, "RCP-20260905-8E9F0A1B", null, null, new DateTime(2026, 9, 3, 14, 0, 0, 0, DateTimeKind.Utc), "BDP-20260903-1F2E3D4C", "Confirmed", new DateTime(2026, 9, 5, 14, 0, 0, 0, DateTimeKind.Utc), 106.60m, 926.60m }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { new Guid("6a7c0141-0c82-5803-bf14-39afd9893f3f"), "payments.finance", "payments.finance" },
                    { new Guid("c2c32ec5-cb1d-c50e-2302-d037eec631e9"), "payment-blocks.manage", "payment-blocks.manage" }
                });

            migrationBuilder.InsertData(
                table: "PaymentDetails",
                columns: new[] { "Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount" },
                values: new object[,]
                {
                    { new Guid("2d670ee7-847a-b99b-7aaf-7c56965a2a3b"), null, 25000m, new Guid("11111111-0007-0007-0007-000000000002"), "Importadora Demo SpA", "76123456-7", "HLCUVAL250200456", "HLCUBKG2502004", "ISPS", "CLP", "ISPS - histórico", null, "LocalCharge", null, 29750m, "CLP", new Guid("55555555-000b-000b-000b-000000000009"), new DateTime(2026, 9, 12, 15, 20, 0, 0, DateTimeKind.Utc), null, 4750m },
                    { new Guid("6b2c23c2-b5a7-d926-50a2-90eb9c24c650"), null, 45000m, new Guid("11111111-0007-0007-0007-000000000006"), "Importadora Demo SpA", "76123456-7", "HLCUSAI260300610", "HLCUBKG2603061", "BL_FEE", "CLP", "BL Documentation Fee (export)", null, "LocalCharge", null, 53550m, "CLP", new Guid("55555555-000b-000b-000b-000000000011"), null, new Guid("33333333-0009-0009-0009-000000000010"), 8550m },
                    { new Guid("6d3b520b-5b65-58aa-66d8-66ff3648e5a6"), null, 820m, new Guid("11111111-0007-0007-0007-000000000005"), "Comercial Altiplano SRL", "1023456017", "HLCUIQQ260200078", "HLCUBKG2602078", "GATE_IN", "BOB", "Gate In - histórico", null, "LocalCharge", null, 926.60m, "BOB", new Guid("55555555-000b-000b-000b-000000000013"), new DateTime(2026, 9, 5, 14, 0, 0, 0, DateTimeKind.Utc), null, 106.60m },
                    { new Guid("cf9509cc-b6c2-728d-6de8-084ee4dc8ee0"), null, 85000m, new Guid("11111111-0007-0007-0007-000000000009"), "Importadora Demo SpA", "76123456-7", "HLCUSAI260400910", "HLCUBKG2604091", "MHD", "CLP", "MHD - HLXU3034001", null, "LocalCharge", null, 101150m, "CLP", new Guid("55555555-000b-000b-000b-000000000012"), null, new Guid("33333333-0009-0009-0009-000000000013"), 16150m },
                    { new Guid("e36dea56-a5e1-3b71-8e9f-56a9c02494dc"), null, 45000m, new Guid("11111111-0007-0007-0007-000000000006"), "Importadora Demo SpA", "76123456-7", "HLCUSAI260300610", "HLCUBKG2603061", "BL_FEE", "CLP", "Emisión de BL - histórico", null, "LocalCharge", null, 53550m, "CLP", new Guid("55555555-000b-000b-000b-000000000009"), new DateTime(2026, 9, 12, 15, 20, 0, 0, DateTimeKind.Utc), null, 8550m },
                    { new Guid("f5437389-1088-31f7-8453-a072eb133450"), null, 4940000m, new Guid("11111111-0007-0007-0007-000000000002"), "Importadora Demo SpA", "76123456-7", "HLCUVAL250200456", "HLCUBKG2502004", "FREIGHT", "CLP", "Flete Busan - Valparaíso", 950m, "Freight", null, 5200m, "USD", new Guid("55555555-000b-000b-000b-000000000010"), new DateTime(2026, 9, 20, 14, 5, 0, 0, DateTimeKind.Utc), new Guid("11111111-0007-0007-0007-000000000002"), 0m }
                });

            migrationBuilder.InsertData(
                table: "PaymentStatusChanges",
                columns: new[] { "Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus" },
                values: new object[,]
                {
                    { new Guid("098ec853-d62a-8529-61a7-c4a940b36b57"), new DateTime(2026, 10, 4, 16, 44, 30, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, new Guid("55555555-000b-000b-000b-000000000012"), null, "Pending" },
                    { new Guid("1650f54c-de7a-f6c0-6ebb-482ea121f9bb"), new DateTime(2026, 9, 3, 14, 0, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), "Pending", new Guid("55555555-000b-000b-000b-000000000013"), "Deposit slip issued", "PendingVerification" },
                    { new Guid("18589bd7-e7f5-2229-c22d-fda464a415b5"), new DateTime(2026, 10, 4, 16, 45, 0, 0, DateTimeKind.Utc), "SYSTEM", null, "Pending", new Guid("55555555-000b-000b-000b-000000000012"), "PROVIDER_UNAVAILABLE", "Failed" },
                    { new Guid("310b1a9e-24ef-a762-ada1-81b38cd80393"), new DateTime(2026, 9, 3, 13, 55, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), null, new Guid("55555555-000b-000b-000b-000000000013"), null, "Pending" },
                    { new Guid("32849ef5-77ee-6b91-da24-9d0d642dd46c"), new DateTime(2026, 9, 20, 14, 2, 0, 0, DateTimeKind.Utc), "agente@maritimpacifico.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000030"), null, new Guid("55555555-000b-000b-000b-000000000010"), null, "Pending" },
                    { new Guid("4afe91c7-53b4-50b0-5ccf-5f4fac86b014"), new DateTime(2026, 10, 3, 13, 20, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, new Guid("55555555-000b-000b-000b-000000000011"), null, "Pending" },
                    { new Guid("710fc5b6-a180-5f74-3ce4-b685465903f0"), new DateTime(2026, 9, 20, 14, 2, 0, 0, DateTimeKind.Utc), "SYSTEM", null, "Pending", new Guid("55555555-000b-000b-000b-000000000010"), "Initiated in BancoChile", "Processing" },
                    { new Guid("b2ace45d-692c-b548-5d2b-4d3d1d7ec3e0"), new DateTime(2026, 9, 12, 15, 20, 0, 0, DateTimeKind.Utc), "KHIPU_WEBHOOK", null, "Processing", new Guid("55555555-000b-000b-000b-000000000009"), null, "Confirmed" },
                    { new Guid("bdaa1d52-0e83-a373-93f7-26891c116e1e"), new DateTime(2026, 9, 12, 15, 18, 0, 0, DateTimeKind.Utc), "SYSTEM", null, "Pending", new Guid("55555555-000b-000b-000b-000000000009"), "Initiated in Khipu", "Processing" },
                    { new Guid("c947ed71-f5c6-e78e-f436-4341e97c31e4"), new DateTime(2026, 10, 3, 13, 30, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Pending", new Guid("55555555-000b-000b-000b-000000000011"), "Deposit slip issued", "PendingVerification" },
                    { new Guid("dab3029d-48f1-ac60-266b-ecb0d1c5f3be"), new DateTime(2026, 9, 12, 15, 18, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, new Guid("55555555-000b-000b-000b-000000000009"), null, "Pending" },
                    { new Guid("e4d4eb7a-b86d-6e75-35cf-39ecdf9d9182"), new DateTime(2026, 9, 20, 14, 5, 0, 0, DateTimeKind.Utc), "BANCOCHILE_WEBHOOK", null, "Processing", new Guid("55555555-000b-000b-000b-000000000010"), null, "Confirmed" },
                    { new Guid("fe438c5a-1da7-bfa9-d71b-7648dccfb8fb"), new DateTime(2026, 9, 5, 14, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), "PendingVerification", new Guid("55555555-000b-000b-000b-000000000013"), null, "Confirmed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ClientId_PaymentDate",
                table: "Payments",
                columns: new[] { "ClientId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedByUserId_IdempotencyKey",
                table: "Payments",
                columns: new[] { "CreatedByUserId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDetails_ItemType_SourceId",
                table: "PaymentDetails",
                columns: new[] { "ItemType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ItemType_SourceId",
                table: "CartItems",
                columns: new[] { "CartId", "ItemType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_LockedByPaymentId",
                table: "CartItems",
                column: "LockedByPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId_OrganizationId",
                table: "Carts",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_BillOfLadingId",
                table: "CustomerInvoices",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_OrganizationId_IssueDate",
                table: "CustomerInvoices",
                columns: new[] { "OrganizationId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_OrganizationId_SourceNumber",
                table: "CustomerInvoices",
                columns: new[] { "OrganizationId", "SourceNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentBlockWindows_IsActive_EndDate",
                table: "PaymentBlockWindows",
                columns: new[] { "IsActive", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCurrencyRules_Country_ConceptCode_Currency",
                table: "PaymentCurrencyRules",
                columns: new[] { "Country", "ConceptCode", "Currency" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodConfigs_Country_Code",
                table: "PaymentMethodConfigs",
                columns: new[] { "Country", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOutboxMessages_PaymentId",
                table: "PaymentOutboxMessages",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOutboxMessages_Status_NextAttemptAt",
                table: "PaymentOutboxMessages",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatusChanges_PaymentId_ChangedAt",
                table: "PaymentStatusChanges",
                columns: new[] { "PaymentId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "CustomerInvoices");

            migrationBuilder.DropTable(
                name: "PaymentBlockWindows");

            migrationBuilder.DropTable(
                name: "PaymentCurrencyRules");

            migrationBuilder.DropTable(
                name: "PaymentMethodConfigs");

            migrationBuilder.DropTable(
                name: "PaymentOutboxMessages");

            migrationBuilder.DropTable(
                name: "PaymentStatusChanges");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ClientId_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CreatedByUserId_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentDetails_ItemType_SourceId",
                table: "PaymentDetails");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0001-0001-000000000004"));

            migrationBuilder.DeleteData(
                table: "ExchangeRateRecords",
                keyColumn: "Id",
                keyValue: new Guid("b3a141c8-4669-b188-6d5a-edaf7cacd5a5"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("009d0f99-085a-ba06-8922-5a0493f16195"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("21fe52a2-53f3-ea6f-16e7-786a3d0e0356"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("25c18b0f-18ba-3150-1be7-88545cabf81d"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("2cf55acb-0012-8fc4-a30b-b8e6b5dfba2f"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("301dfc03-3a4c-659e-f88b-048be09d4b13"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("3153499e-1612-eefd-7af8-7804b5d8f54d"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("3b8aa0fa-17d0-516c-5957-2018b8e21757"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("48c2038f-b24f-3b7f-1210-08fc6d7c45df"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("5f20b6bf-6258-7dfc-9337-83786ed0c2e6"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("67823a49-3a13-1a79-15f6-097407733cc1"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("6e624596-577b-cfa9-7f30-a43d2de5f5cd"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("6ee41684-f5b7-0176-23ed-ab6b94f0815f"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("7126d08c-77ab-5a25-a793-ea20c39fbf0e"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("8806e939-b593-2530-fda8-1e06219d03c3"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("9516cb88-c63f-aee3-2979-8ac69d5688c9"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("963f22f1-c90b-4145-9d68-d23b271dbef5"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("9707c44c-468c-fba3-bac1-d2b3e24d55a6"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("99e1ee9a-4cb8-6a8f-7834-61b67cca8c41"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("a006e2af-63e5-00bb-7f00-8741b4d8b17e"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("a0d3fb65-c917-d6fe-11d7-97d7ab5fa000"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("a817ee90-0e4b-823a-f85e-86d26390a1f6"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("bcafbb5c-b285-5fd1-9598-f72a60f20ca7"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("cdeac7b5-d7b7-92d9-a901-a94bdedd39ab"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("d0f6487e-45c1-3e34-5a1d-26a1d0ed6f70"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("d271dc82-d532-ec69-794c-0d0d2193e12f"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("e0b8fcc8-f790-c0b9-6959-b0c38b58753c"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("2d670ee7-847a-b99b-7aaf-7c56965a2a3b"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("6b2c23c2-b5a7-d926-50a2-90eb9c24c650"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("6d3b520b-5b65-58aa-66d8-66ff3648e5a6"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("cf9509cc-b6c2-728d-6de8-084ee4dc8ee0"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("e36dea56-a5e1-3b71-8e9f-56a9c02494dc"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("f5437389-1088-31f7-8453-a072eb133450"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("6a7c0141-0c82-5803-bf14-39afd9893f3f"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c2c32ec5-cb1d-c50e-2302-d037eec631e9"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000009"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000010"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000011"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000012"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000013"));

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancelledByRole",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayerName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayerTaxId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentMethodCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderTransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RedirectUrl",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RequestFingerprint",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SlipIssuedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SlipNumber",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AccessGrantId",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "BillOfLadingId",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "BillingName",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "BillingTaxId",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "BlNumber",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "BookingNumber",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "OnBehalfOfClientId",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "OriginalCurrency",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "FreightPaidAt",
                table: "BillsOfLading");

            migrationBuilder.AlterColumn<Guid>(
                name: "BillOfLadingId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ClientId",
                table: "Payments",
                column: "ClientId");
        }
    }
}
