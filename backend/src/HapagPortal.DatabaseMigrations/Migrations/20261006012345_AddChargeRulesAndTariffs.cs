using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddChargeRulesAndTariffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseChanges_BillOfLadingId",
                table: "WarehouseChanges");

            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "WarehouseChanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "WarehouseChanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerNumber",
                table: "WarehouseChanges",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntitlementReference",
                table: "WarehouseChanges",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntitlementSource",
                table: "WarehouseChanges",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "WarehouseChanges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByClientId",
                table: "WarehouseChanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByUserId",
                table: "WarehouseChanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TariffCode",
                table: "WarehouseChanges",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TariffSource",
                table: "WarehouseChanges",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDueDate",
                table: "DemurrageCharges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "DemurrageCharges",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoicedAt",
                table: "DemurrageCharges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppliedExemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalChargeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExemptParty = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PartyTaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PartyMatchCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ExemptAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ConditionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ConditionCurrency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    ConditionValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ConditionValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PayerClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppliedExemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppliedExemptions_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChargeConcepts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Countries = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NexusTariff = table.Column<bool>(type: "boolean", nullable: false),
                    NexusExemptible = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ChargeConcepts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRateRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ToCurrency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    SourceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConvertedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InternalChargeRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MatchCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MaxUsesPerBl = table.Column<int>(type: "integer", nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_InternalChargeRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintainerChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Maintainer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreviousValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintainerChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tariffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ContainerType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TierUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TierMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_Tariffs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseChangeBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    ProcessedItems = table.Column<int>(type: "integer", nullable: false),
                    SucceededItems = table.Column<int>(type: "integer", nullable: false),
                    FailedItems = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseChangeBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TariffTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TariffId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUnit = table.Column<int>(type: "integer", nullable: false),
                    ToUnit = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TariffTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TariffTiers_Tariffs_TariffId",
                        column: x => x.TariffId,
                        principalTable: "Tariffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseChangeBatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContainerNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FromWarehouse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ToWarehouse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TariffCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WarehouseChangeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseChangeBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseChangeBatchItems_WarehouseChangeBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "WarehouseChangeBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000009"), "HLCUSAI260400910", null, "HLCUBKG2604091", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), 4200m, "USD", null, null, false, false, null, null, null, null, null, "Santiago, Chile", "San Antonio (CLSAI)", "Hamburg (DEHAM)", "Import", "Hamburg Industrial Supplies GmbH", "Arrived", "Rio de Janeiro Express", null, "2608E" },
                    { new Guid("11111111-0007-0007-0007-000000000010"), "HLCUSAI260401020", "Master", "HLCUBKG2604102", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Delfin Logística SpA", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3100m, "USD", null, null, false, false, null, null, null, null, null, "Santiago, Chile", "San Antonio (CLSAI)", "Ningbo (CNNGB)", "Import", "Ningbo Home Goods Co.", "Arrived", "Valparaiso Express", null, "2609E" }
                });

            migrationBuilder.InsertData(
                table: "BusinessHolidays",
                columns: new[] { "Id", "Country", "Date", "Name" },
                values: new object[,]
                {
                    { new Guid("00667b14-f55b-c1ab-079d-cfaf973e784f"), "BO", new DateOnly(2026, 1, 1), "Año Nuevo" },
                    { new Guid("0345893c-bf3c-20b2-929c-4c6760c12ea6"), "CL", new DateOnly(2026, 9, 18), "Independencia Nacional" },
                    { new Guid("04647fd3-474f-5b22-d730-b54109e14665"), "CL", new DateOnly(2026, 8, 15), "Asunción de la Virgen" },
                    { new Guid("0921c177-75bf-dc52-bcde-695270c9da2b"), "CL", new DateOnly(2026, 1, 1), "Año Nuevo" },
                    { new Guid("0b11ee5e-ed51-d918-8fd8-86819bad595b"), "CL", new DateOnly(2026, 7, 16), "Virgen del Carmen" },
                    { new Guid("1fe1f1c8-cc7a-56d2-a4d5-b4300f67b825"), "BO", new DateOnly(2026, 4, 3), "Viernes Santo" },
                    { new Guid("25f095d5-b815-34db-4917-f01f716a83a4"), "BO", new DateOnly(2026, 11, 2), "Día de Todos los Difuntos" },
                    { new Guid("2bf84b88-d4f8-89e6-50ed-ef3e8446b9d7"), "CL", new DateOnly(2026, 4, 4), "Sábado Santo" },
                    { new Guid("375bf084-1ff5-be44-5b63-78ed3951d5cc"), "CL", new DateOnly(2026, 9, 19), "Glorias del Ejército" },
                    { new Guid("554e98ef-1bbc-9240-df24-5df13af1398f"), "BO", new DateOnly(2026, 6, 21), "Año Nuevo Andino Amazónico" },
                    { new Guid("56b5e7cc-26d5-c636-17c9-121b6a9951ff"), "BO", new DateOnly(2026, 8, 6), "Día de la Independencia" },
                    { new Guid("5c0a0896-aa52-757f-2738-df5b08cbba02"), "CL", new DateOnly(2026, 10, 31), "Día de las Iglesias Evangélicas" },
                    { new Guid("5f3c085f-eaad-76cb-8bc0-ec225e6b6af3"), "CL", new DateOnly(2026, 12, 8), "Inmaculada Concepción" },
                    { new Guid("61669654-f613-9ae9-e1a5-cdc9a5314569"), "CL", new DateOnly(2026, 10, 12), "Encuentro de Dos Mundos" },
                    { new Guid("6dd776f4-26aa-9450-5270-b96aa13d7e3f"), "BO", new DateOnly(2026, 2, 17), "Carnaval" },
                    { new Guid("70795c68-c039-4652-1bf2-ae781c1991bf"), "CL", new DateOnly(2026, 6, 29), "San Pedro y San Pablo" },
                    { new Guid("70913438-b0f5-b158-5551-b21fb3ec7f06"), "BO", new DateOnly(2026, 5, 1), "Día del Trabajo" },
                    { new Guid("7a29e195-fc46-f98f-543c-411f66e2769c"), "BO", new DateOnly(2026, 1, 22), "Día del Estado Plurinacional" },
                    { new Guid("904e1cbd-fff3-71ee-0e11-51ffddcdcb0a"), "BO", new DateOnly(2026, 2, 16), "Carnaval" },
                    { new Guid("96dab692-42ad-702b-ff41-67c84a038868"), "CL", new DateOnly(2026, 12, 25), "Navidad" },
                    { new Guid("9b73b249-c4b8-8bad-7600-b824e9f8b535"), "CL", new DateOnly(2026, 11, 1), "Día de Todos los Santos" },
                    { new Guid("c9119139-f40f-ce2b-3016-b5fd7f6eabf9"), "CL", new DateOnly(2026, 5, 21), "Día de las Glorias Navales" },
                    { new Guid("d8361636-b0a2-28a8-00e4-f0d30ed0c028"), "BO", new DateOnly(2026, 12, 25), "Navidad" },
                    { new Guid("ea454567-9647-98ec-1622-1e19115576c7"), "CL", new DateOnly(2026, 5, 1), "Día del Trabajo" },
                    { new Guid("edd6c2a7-a705-72e7-7701-5f5f850193b5"), "CL", new DateOnly(2026, 4, 3), "Viernes Santo" },
                    { new Guid("f03996d9-d5d3-b056-8dad-6a2d2e4c02c4"), "BO", new DateOnly(2026, 6, 4), "Corpus Christi" }
                });

            migrationBuilder.InsertData(
                table: "ChargeConcepts",
                columns: new[] { "Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff" },
                values: new object[,]
                {
                    { new Guid("1da4f295-4e5f-e846-1734-a1aa14450474"), "LocalCharge", "GATE_OUT", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 30, true, null, null, "Gate Out", true, false },
                    { new Guid("24bc9c98-bd06-08fb-4e1a-cadf22532fc1"), "LocalCharge", "IPO", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 40, true, null, null, "Recargo IPO", false, true },
                    { new Guid("2d69da90-bad5-0a9d-8dbd-c19af4d4dd3f"), "LocalCharge", "TRANSIT_FEE", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 90, true, null, null, "Documentación de tránsito Bolivia", false, false },
                    { new Guid("2f0b15d3-5a45-9082-ca75-e3498d34d226"), "Demurrage", "ADVANCE_DEMURRAGE_BO", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 120, true, null, null, "Demoras anticipadas", false, false },
                    { new Guid("31440d52-413f-6736-6b57-cfa6bd491b3a"), "Demurrage", "DEMURRAGE", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 110, true, null, null, "Demurrage (sobreestadía)", false, false },
                    { new Guid("55630f14-a5e2-fc47-0de6-3eee5fa6b0ff"), "LocalCharge", "ISPS", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 80, true, null, null, "Recargo de seguridad ISPS", false, false },
                    { new Guid("5f2a94ad-43d2-ecc1-039d-ce031cc98373"), "LocalCharge", "EDS", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 20, true, null, null, "EDS", true, false },
                    { new Guid("5f6f4be2-432f-bac4-ff92-4619f9d91214"), "LocalCharge", "BL_FEE", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 70, true, null, null, "Emisión de BL", false, false },
                    { new Guid("61652e31-3c9a-7798-fa4d-80996f4703a6"), "Service", "LATE_ARRIVAL", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 140, true, null, null, "Late Arrival", false, false },
                    { new Guid("7ff3d950-52ea-607a-68ea-3ef09e8345c1"), "LocalCharge", "THC_RF", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 60, true, null, null, "Terminal Handling Charge Reefer", false, false },
                    { new Guid("a2d1533d-a372-0df8-52cc-17a7b169e953"), "LocalCharge", "THC", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 50, true, null, null, "Terminal Handling Charge", false, false },
                    { new Guid("b6480268-68ac-c09e-0f6b-3335d8cd7b7e"), "LocalCharge", "GATE_IN", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 10, true, null, null, "Gate In", true, false },
                    { new Guid("e6a87f12-1a29-20ca-f10f-2dc2b24d74ed"), "Service", "WAREHOUSE_CHANGE", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 130, true, null, null, "Cambio de almacén", false, true },
                    { new Guid("fa247c08-999c-0d02-1d37-708d43c0a03c"), "Demurrage", "MHD", "CL,BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 100, true, null, null, "MHD", false, false }
                });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy" },
                values: new object[,]
                {
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000060"), null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Client", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "contacto@distribuidoraandes.cl", true, true, "MC000202", null, null, "Distribuidora Andes Crédito SpA", "CL", "Customer", null, "Approved", null, null, "76.000.002-2", "RUT", null, null },
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000070"), null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Client", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "operaciones@globalforwarding.cl", true, true, "MC000303", null, null, "Global Forwarding Chile SpA", "CL", "FreightForwarder", null, "Approved", null, null, "76.000.003-3", "RUT", null, null }
                });

            migrationBuilder.UpdateData(
                table: "DemurrageCharges",
                keyColumn: "Id",
                keyValue: new Guid("44444444-000a-000a-000a-000000000001"),
                columns: new[] { "InvoiceDueDate", "InvoiceNumber", "InvoicedAt" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DemurrageCharges",
                keyColumn: "Id",
                keyValue: new Guid("44444444-000a-000a-000a-000000000002"),
                columns: new[] { "InvoiceDueDate", "InvoiceNumber", "InvoicedAt" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DemurrageCharges",
                keyColumn: "Id",
                keyValue: new Guid("44444444-000a-000a-000a-000000000003"),
                columns: new[] { "InvoiceDueDate", "InvoiceNumber", "InvoicedAt" },
                values: new object[] { null, null, null });

            migrationBuilder.InsertData(
                table: "InternalChargeRules",
                columns: new[] { "Id", "AccountName", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "MatchCode", "MaxUsesPerBl", "ModifiedAt", "ModifiedBy", "Reason", "RuleType", "TaxId", "ValidFrom", "ValidTo" },
                values: new object[,]
                {
                    { new Guid("eeeeeeee-0015-0015-0015-000000000001"), "Importadora Demo SpA", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "MC100010", 1, null, null, "Convenio comercial: un cambio de almacén gratuito por BL", "FreeWarehouseChange", "76123456-7", new DateOnly(2026, 1, 1), null },
                    { new Guid("eeeeeeee-0015-0015-0015-000000000002"), "Comercial Altiplano SRL", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "MC100020", null, null, null, "Regla interna Bolivia: demoras anticipadas obligatorias antes del CLD", "AdvanceDemurrageRequired", "1023456017", new DateOnly(2026, 1, 1), null }
                });

            migrationBuilder.UpdateData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000009"),
                column: "ChargeType",
                value: "GATE_IN");

            migrationBuilder.UpdateData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000012"),
                column: "ChargeType",
                value: "GATE_IN");

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000016"), 150m, new Guid("11111111-0007-0007-0007-000000000001"), "IPO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Recargo IPO", false, null, null, "Pending", 0m, 0m, 150m },
                    { new Guid("33333333-0009-0009-0009-000000000017"), 85000m, new Guid("11111111-0007-0007-0007-000000000001"), "MHD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "MHD - HLXU1234567 / HLXU7654321", true, null, null, "Pending", 16150m, 19m, 101150m },
                    { new Guid("33333333-0009-0009-0009-000000000021"), 450m, new Guid("11111111-0007-0007-0007-000000000004"), "MHD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "MHD - HLXU8899001", false, null, null, "Pending", 0m, 0m, 450m },
                    { new Guid("33333333-0009-0009-0009-000000000022"), 60000m, new Guid("11111111-0007-0007-0007-000000000006"), "GATE_OUT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Gate Out - 40RF (San Antonio)", true, null, null, "Pending", 11400m, 19m, 71400m }
                });

            migrationBuilder.InsertData(
                table: "MaintainerChangeLogs",
                columns: new[] { "Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue" },
                values: new object[,]
                {
                    { new Guid("00262c9e-81f8-3ef1-63fc-3e4e25484381"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000002"), "Tariff", "{\"conceptCode\":\"WAREHOUSE_CHANGE\",\"code\":\"KTF\",\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":null,\"description\":\"Cambio de almac\\u00E9n (KTF)\",\"amount\":110910,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("4af7222d-2559-d657-c911-d47dc1843208"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000006"), "Tariff", "{\"conceptCode\":\"DEMURRAGE\",\"code\":null,\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":null,\"description\":\"Demurrage 40\\u0027 y especiales por d\\u00EDa desde la descarga\",\"amount\":0,\"tierUnit\":\"CalendarDays\",\"tierMode\":\"PerUnit\",\"tiers\":[{\"fromUnit\":1,\"toUnit\":7,\"amount\":0},{\"fromUnit\":8,\"toUnit\":14,\"amount\":45000},{\"fromUnit\":15,\"toUnit\":null,\"amount\":65000}],\"validFrom\":\"2026-01-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("5bf554e0-9a9d-0205-6b6a-9a82eb8f2e8f"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000001"), "Tariff", "{\"conceptCode\":\"WAREHOUSE_CHANGE\",\"code\":\"KTE\",\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":null,\"description\":\"Cambio de almac\\u00E9n (KTE)\",\"amount\":9940,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("5ccf1aaa-2d67-6507-44f1-d222a6bd2de0"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000008"), "Tariff", "{\"conceptCode\":\"ADVANCE_DEMURRAGE_BO\",\"code\":null,\"country\":\"BO\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Demoras anticipadas por contenedor\",\"amount\":150,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-01-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("6b2cd9c6-cb6f-38cb-c161-b531902c4f5f"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000005"), "Tariff", "{\"conceptCode\":\"DEMURRAGE\",\"code\":null,\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":\"20DV\",\"description\":\"Demurrage 20\\u0027 por d\\u00EDa desde la descarga\",\"amount\":0,\"tierUnit\":\"CalendarDays\",\"tierMode\":\"PerUnit\",\"tiers\":[{\"fromUnit\":1,\"toUnit\":7,\"amount\":0},{\"fromUnit\":8,\"toUnit\":14,\"amount\":35000},{\"fromUnit\":15,\"toUnit\":null,\"amount\":50000}],\"validFrom\":\"2026-01-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("7ab0cffc-981f-77ae-99a1-b8b068b9f60e"), "Created", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0015-0015-0015-000000000002"), "InternalChargeRule", "{\"ruleType\":\"AdvanceDemurrageRequired\",\"country\":\"BO\",\"taxId\":\"1023456017\",\"matchCode\":\"MC100020\",\"accountName\":\"Comercial Altiplano SRL\",\"reason\":\"Regla interna Bolivia: demoras anticipadas obligatorias antes del CLD\",\"maxUsesPerBl\":null,\"validFrom\":\"2026-01-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("836c5b07-4eed-d9cf-301e-0b003b44e4ed"), "Created", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0015-0015-0015-000000000001"), "InternalChargeRule", "{\"ruleType\":\"FreeWarehouseChange\",\"country\":\"CL\",\"taxId\":\"76123456-7\",\"matchCode\":\"MC100010\",\"accountName\":\"Importadora Demo SpA\",\"reason\":\"Convenio comercial: un cambio de almac\\u00E9n gratuito por BL\",\"maxUsesPerBl\":1,\"validFrom\":\"2026-01-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("b7780317-c00e-3ebe-3d0b-606bf60647bf"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000007"), "Tariff", "{\"conceptCode\":\"DEMURRAGE\",\"code\":null,\"country\":\"BO\",\"currency\":\"BOB\",\"containerType\":null,\"description\":\"Demurrage Bolivia por d\\u00EDa desde la descarga\",\"amount\":0,\"tierUnit\":\"CalendarDays\",\"tierMode\":\"PerUnit\",\"tiers\":[{\"fromUnit\":1,\"toUnit\":10,\"amount\":0},{\"fromUnit\":11,\"toUnit\":20,\"amount\":310},{\"fromUnit\":21,\"toUnit\":null,\"amount\":450}],\"validFrom\":\"2026-01-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("c0f7f17b-bf2c-d265-7870-e857f36d9ce5"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000003"), "Tariff", "{\"conceptCode\":\"WAREHOUSE_CHANGE\",\"code\":null,\"country\":\"BO\",\"currency\":\"BOB\",\"containerType\":null,\"description\":\"Cambio de almac\\u00E9n Bolivia\",\"amount\":850,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-01-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("f2dac0e5-7721-dda5-b0cb-e740dcce6e9d"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000004"), "Tariff", "{\"conceptCode\":\"LATE_ARRIVAL\",\"code\":null,\"country\":\"CL\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Late Arrival por horas desde el cierre de recepci\\u00F3n\",\"amount\":0,\"tierUnit\":\"Hours\",\"tierMode\":\"Flat\",\"tiers\":[{\"fromUnit\":0,\"toUnit\":24,\"amount\":100},{\"fromUnit\":25,\"toUnit\":48,\"amount\":200},{\"fromUnit\":49,\"toUnit\":null,\"amount\":350}],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null }
                });

            migrationBuilder.InsertData(
                table: "Tariffs",
                columns: new[] { "Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo" },
                values: new object[,]
                {
                    { new Guid("eeeeeeee-0014-0014-0014-000000000001"), 9940m, "KTE", "WAREHOUSE_CHANGE", null, "CL", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Cambio de almacén (KTE)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("eeeeeeee-0014-0014-0014-000000000002"), 110910m, "KTF", "WAREHOUSE_CHANGE", null, "CL", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Cambio de almacén (KTF)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("eeeeeeee-0014-0014-0014-000000000003"), 850m, null, "WAREHOUSE_CHANGE", null, "BO", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Cambio de almacén Bolivia", true, null, null, "Flat", "None", new DateOnly(2026, 1, 1), null },
                    { new Guid("eeeeeeee-0014-0014-0014-000000000004"), 0m, null, "LATE_ARRIVAL", null, "CL", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Late Arrival por horas desde el cierre de recepción", true, null, null, "Flat", "Hours", new DateOnly(2026, 10, 1), null },
                    { new Guid("eeeeeeee-0014-0014-0014-000000000005"), 0m, null, "DEMURRAGE", "20DV", "CL", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Demurrage 20' por día desde la descarga", true, null, null, "PerUnit", "CalendarDays", new DateOnly(2026, 1, 1), null },
                    { new Guid("eeeeeeee-0014-0014-0014-000000000006"), 0m, null, "DEMURRAGE", null, "CL", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Demurrage 40' y especiales por día desde la descarga", true, null, null, "PerUnit", "CalendarDays", new DateOnly(2026, 1, 1), null },
                    { new Guid("eeeeeeee-0014-0014-0014-000000000007"), 0m, null, "DEMURRAGE", null, "BO", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Demurrage Bolivia por día desde la descarga", true, null, null, "PerUnit", "CalendarDays", new DateOnly(2026, 1, 1), null },
                    { new Guid("eeeeeeee-0014-0014-0014-000000000008"), 150m, null, "ADVANCE_DEMURRAGE_BO", null, "BO", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Demoras anticipadas por contenedor", true, null, null, "Flat", "None", new DateOnly(2026, 1, 1), null }
                });

            migrationBuilder.UpdateData(
                table: "WarehouseChanges",
                keyColumn: "Id",
                keyValue: new Guid("99999999-000f-000f-000f-000000000001"),
                columns: new[] { "BatchId", "CompletedAt", "ContainerNumber", "EntitlementReference", "EntitlementSource", "IsFree", "RequestedByClientId", "RequestedByUserId", "TariffCode", "TariffSource" },
                values: new object[] { null, null, null, null, null, false, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "WarehouseChanges",
                keyColumn: "Id",
                keyValue: new Guid("99999999-000f-000f-000f-000000000002"),
                columns: new[] { "BatchId", "CompletedAt", "ContainerNumber", "EntitlementReference", "EntitlementSource", "IsFree", "RequestedByClientId", "RequestedByUserId", "TariffCode", "TariffSource" },
                values: new object[] { null, null, null, null, null, false, null, null, null, null });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000011"), new Guid("11111111-0007-0007-0007-000000000009"), "HLXU3034001", "40HC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-030401", "Discharged", null, null, 23900m },
                    { new Guid("22222222-0008-0008-0008-000000000012"), new Guid("11111111-0007-0007-0007-000000000010"), "HLXU3034002", "20DV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-030402", "Discharged", null, null, 16800m },
                    { new Guid("22222222-0008-0008-0008-000000000013"), new Guid("11111111-0007-0007-0007-000000000010"), "HLXU3034003", "40HC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-030403", "Discharged", null, null, 24100m }
                });

            migrationBuilder.InsertData(
                table: "BLParties",
                columns: new[] { "Id", "Address", "BillOfLadingId", "CountryCode", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Name", "Role", "TaxId", "TaxIdType" },
                values: new object[] { new Guid("dddddddd-0013-0013-0013-000000000001"), null, new Guid("11111111-0007-0007-0007-000000000010"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Delfin Logística SpA", "Consignee", "76000001-1", "RUT" });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage" },
                values: new object[] { new Guid("11111111-0007-0007-0007-000000000011"), "HLCUVAP260401130", null, "HLCUBKG2604113", new Guid("c3d4e5f6-0003-0003-0003-000000000060"), "Global Forwarding Chile SpA", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 9, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), 2600m, "USD", null, null, false, false, null, null, null, null, null, "Valparaiso, Chile", "Valparaiso (CLVAP)", "Santos (BRSSZ)", "Import", "Santos Coffee Exporters Ltda", "Arrived", "Santos Express", null, "2609N" });

            migrationBuilder.InsertData(
                table: "DemurrageCharges",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "CreatedAt", "CreatedBy", "Currency", "DailyRate", "DeletedAt", "DeletedBy", "DemurrageDays", "EndDate", "ExemptReason", "FreeDays", "InvoiceDueDate", "InvoiceNumber", "InvoicedAt", "IsExempt", "ModifiedAt", "ModifiedBy", "StartDate", "Status", "TotalAmount" },
                values: new object[] { new Guid("44444444-000a-000a-000a-000000000004"), new Guid("11111111-0007-0007-0007-000000000009"), "HLXU3034001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", 51000m, null, null, 10, new DateTime(2026, 9, 6, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, new DateTime(2026, 10, 15, 3, 0, 0, 0, DateTimeKind.Utc), "FAC-DEM-2026-0915", new DateTime(2026, 9, 15, 12, 0, 0, 0, DateTimeKind.Utc), false, null, null, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Invoiced", 510000m });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000013"), 85000m, new Guid("11111111-0007-0007-0007-000000000009"), "MHD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "MHD - HLXU3034001", true, null, null, "Pending", 16150m, 19m, 101150m },
                    { new Guid("33333333-0009-0009-0009-000000000014"), 95000m, new Guid("11111111-0007-0007-0007-000000000010"), "GATE_IN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Gate In - devolución de vacíos (San Antonio)", true, null, null, "Pending", 18050m, 19m, 113050m },
                    { new Guid("33333333-0009-0009-0009-000000000015"), 38000m, new Guid("11111111-0007-0007-0007-000000000010"), "EDS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "EDS", true, null, null, "Pending", 7220m, 19m, 45220m }
                });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[,]
                {
                    { new Guid("6b4eb3e7-26d3-48e5-8ad2-f1f5822756a3"), new Guid("11111111-0007-0007-0007-000000000009"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" },
                    { new Guid("ccf652e1-278e-3ce7-eb83-c2a14d706449"), new Guid("11111111-0007-0007-0007-000000000010"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" }
                });

            migrationBuilder.InsertData(
                table: "TariffTiers",
                columns: new[] { "Id", "Amount", "FromUnit", "TariffId", "ToUnit" },
                values: new object[,]
                {
                    { new Guid("2261b5bc-635c-2ddc-27a9-bf7d66747d55"), 450m, 21, new Guid("eeeeeeee-0014-0014-0014-000000000007"), null },
                    { new Guid("49ba8cc8-a39b-5996-22f0-a1ddfcacc0fc"), 310m, 11, new Guid("eeeeeeee-0014-0014-0014-000000000007"), 20 },
                    { new Guid("5fe677cf-b251-4573-d54b-a395044f3f66"), 0m, 1, new Guid("eeeeeeee-0014-0014-0014-000000000005"), 7 },
                    { new Guid("64243965-3f4c-060c-c6d0-f4ac9992012a"), 45000m, 8, new Guid("eeeeeeee-0014-0014-0014-000000000006"), 14 },
                    { new Guid("674264eb-2ffd-1ddc-0d52-be33d9bd0469"), 50000m, 15, new Guid("eeeeeeee-0014-0014-0014-000000000005"), null },
                    { new Guid("70dbb6e7-87a3-2817-6346-f9879a242a2f"), 65000m, 15, new Guid("eeeeeeee-0014-0014-0014-000000000006"), null },
                    { new Guid("76a040c4-94e3-5e10-9026-016e4521572a"), 0m, 1, new Guid("eeeeeeee-0014-0014-0014-000000000007"), 10 },
                    { new Guid("99179f3b-5c8d-40e8-6571-1673dc54b54d"), 35000m, 8, new Guid("eeeeeeee-0014-0014-0014-000000000005"), 14 },
                    { new Guid("a11c263f-1a4f-4b44-6a53-28b1f7182f35"), 0m, 1, new Guid("eeeeeeee-0014-0014-0014-000000000006"), 7 },
                    { new Guid("a9820f36-7222-b459-9737-1738a226406c"), 200m, 25, new Guid("eeeeeeee-0014-0014-0014-000000000004"), 48 },
                    { new Guid("c2061609-752b-0914-6d80-3d88b0ba4978"), 100m, 0, new Guid("eeeeeeee-0014-0014-0014-000000000004"), 24 },
                    { new Guid("deb552c4-3437-7294-3ce0-592446a7712e"), 350m, 49, new Guid("eeeeeeee-0014-0014-0014-000000000004"), null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username" },
                values: new object[,]
                {
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000060"), new Guid("c3d4e5f6-0003-0003-0003-000000000060"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "credito@distribuidoraandes.cl", null, null, "Camila", true, false, null, "Crédito", null, null, "Active", null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, null, "Client", "credito@distribuidoraandes.cl" },
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000070"), new Guid("c3d4e5f6-0003-0003-0003-000000000070"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "ffww@globalforwarding.cl", null, null, "Felipe", true, false, null, "Forwarder", null, null, "Active", null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, null, "Client", "ffww@globalforwarding.cl" }
                });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[] { new Guid("22222222-0008-0008-0008-000000000014"), new Guid("11111111-0007-0007-0007-000000000011"), "HLXU3034004", "20DV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-030404", "Discharged", null, null, 17200m });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000018"), 185000m, new Guid("11111111-0007-0007-0007-000000000011"), "THC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 20DV", true, null, null, "Pending", 35150m, 19m, 220150m },
                    { new Guid("33333333-0009-0009-0009-000000000019"), 150m, new Guid("11111111-0007-0007-0007-000000000011"), "IPO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Recargo IPO", false, null, null, "Pending", 0m, 0m, 150m },
                    { new Guid("33333333-0009-0009-0009-000000000020"), 60000m, new Guid("11111111-0007-0007-0007-000000000011"), "GATE_OUT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Gate Out - 20DV (Valparaíso)", true, null, null, "Pending", 11400m, 19m, 71400m }
                });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[] { new Guid("764cdd78-0b15-3757-b5a8-5f806a2e432e"), new Guid("11111111-0007-0007-0007-000000000011"), new Guid("c3d4e5f6-0003-0003-0003-000000000070"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "RoleId", "RoleName", "UserId" },
                values: new object[,]
                {
                    { new Guid("9669f2e2-7ad8-52c7-ad43-b348fecd7d85"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new Guid("d4e5f6a7-0004-0004-0004-000000000070") },
                    { new Guid("f9a0f3f4-6698-e1c1-73e6-b36bbf9f69e1"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new Guid("d4e5f6a7-0004-0004-0004-000000000060") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseChanges_BatchId",
                table: "WarehouseChanges",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseChanges_BillOfLadingId_IsFree",
                table: "WarehouseChanges",
                columns: new[] { "BillOfLadingId", "IsFree" });

            migrationBuilder.CreateIndex(
                name: "IX_AppliedExemptions_BillOfLadingId",
                table: "AppliedExemptions",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_AppliedExemptions_LocalChargeId",
                table: "AppliedExemptions",
                column: "LocalChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHolidays_Country_Date",
                table: "BusinessHolidays",
                columns: new[] { "Country", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChargeConcepts_Code",
                table: "ChargeConcepts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateRecords_TransactionType_TransactionId",
                table: "ExchangeRateRecords",
                columns: new[] { "TransactionType", "TransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_InternalChargeRules_RuleType_Country_IsActive",
                table: "InternalChargeRules",
                columns: new[] { "RuleType", "Country", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintainerChangeLogs_ChangedAt",
                table: "MaintainerChangeLogs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MaintainerChangeLogs_Maintainer_EntityId_ChangedAt",
                table: "MaintainerChangeLogs",
                columns: new[] { "Maintainer", "EntityId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tariffs_ConceptCode_Country_IsActive_ValidFrom",
                table: "Tariffs",
                columns: new[] { "ConceptCode", "Country", "IsActive", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_TariffTiers_TariffId_FromUnit",
                table: "TariffTiers",
                columns: new[] { "TariffId", "FromUnit" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseChangeBatches_ClientId",
                table: "WarehouseChangeBatches",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseChangeBatches_Status_CreatedAt",
                table: "WarehouseChangeBatches",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseChangeBatchItems_BatchId_Status_LineNumber",
                table: "WarehouseChangeBatchItems",
                columns: new[] { "BatchId", "Status", "LineNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppliedExemptions");

            migrationBuilder.DropTable(
                name: "BusinessHolidays");

            migrationBuilder.DropTable(
                name: "ChargeConcepts");

            migrationBuilder.DropTable(
                name: "ExchangeRateRecords");

            migrationBuilder.DropTable(
                name: "InternalChargeRules");

            migrationBuilder.DropTable(
                name: "MaintainerChangeLogs");

            migrationBuilder.DropTable(
                name: "TariffTiers");

            migrationBuilder.DropTable(
                name: "WarehouseChangeBatchItems");

            migrationBuilder.DropTable(
                name: "Tariffs");

            migrationBuilder.DropTable(
                name: "WarehouseChangeBatches");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseChanges_BatchId",
                table: "WarehouseChanges");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseChanges_BillOfLadingId_IsFree",
                table: "WarehouseChanges");

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000011"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000012"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000013"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000014"));

            migrationBuilder.DeleteData(
                table: "BLParties",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0013-0013-0013-000000000001"));

            migrationBuilder.DeleteData(
                table: "DemurrageCharges",
                keyColumn: "Id",
                keyValue: new Guid("44444444-000a-000a-000a-000000000004"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000013"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000014"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000015"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000016"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000017"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000018"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000019"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000020"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000021"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000022"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("6b4eb3e7-26d3-48e5-8ad2-f1f5822756a3"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("764cdd78-0b15-3757-b5a8-5f806a2e432e"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("ccf652e1-278e-3ce7-eb83-c2a14d706449"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("9669f2e2-7ad8-52c7-ad43-b348fecd7d85"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("f9a0f3f4-6698-e1c1-73e6-b36bbf9f69e1"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000009"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000010"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000011"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000060"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000070"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000060"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000070"));

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "ContainerNumber",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "EntitlementReference",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "EntitlementSource",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "IsFree",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "RequestedByClientId",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "TariffCode",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "TariffSource",
                table: "WarehouseChanges");

            migrationBuilder.DropColumn(
                name: "InvoiceDueDate",
                table: "DemurrageCharges");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "DemurrageCharges");

            migrationBuilder.DropColumn(
                name: "InvoicedAt",
                table: "DemurrageCharges");

            migrationBuilder.UpdateData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000009"),
                column: "ChargeType",
                value: "GateIn");

            migrationBuilder.UpdateData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000012"),
                column: "ChargeType",
                value: "GateIn");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseChanges_BillOfLadingId",
                table: "WarehouseChanges",
                column: "BillOfLadingId");
        }
    }
}
