using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalAssistantAndPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DifuCode",
                table: "BillsOfLading",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DifuLocationCode",
                table: "BillsOfLading",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EblPlatform",
                table: "BillsOfLading",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalDestinationCode",
                table: "BillsOfLading",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuanceStatus",
                table: "BillsOfLading",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuanceStatusAt",
                table: "BillsOfLading",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortOfDischargeCode",
                table: "BillsOfLading",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransportDocumentType",
                table: "BillsOfLading",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssistantMailboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Topic = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_AssistantMailboxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssistantSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EngineMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TranscriptSentTo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TranscriptSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DangerousGoods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UnNumber = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    ProperShippingNameEs = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProperShippingNameEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    HazardClass = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    SubsidiaryRisk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PackingGroup = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsClassified = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SearchText = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
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
                    table.PrimaryKey("PK_DangerousGoods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Topic = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SourceFaqId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentPublicationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    FinalDestinationCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FinalDestinationName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DischargePortCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_ShipmentPublicationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TatcBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    LocationCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceRequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    AcceptedItems = table.Column<int>(type: "integer", nullable: false),
                    RejectedItems = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TatcBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssistantMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Intent = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AnswerType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CitationsJson = table.Column<string>(type: "text", nullable: true),
                    ActionsJson = table.Column<string>(type: "text", nullable: true),
                    Engine = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EngineFallback = table.Column<bool>(type: "boolean", nullable: false),
                    ElapsedMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantMessages_AssistantSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AssistantSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TatcBatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TatcBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TatcBatchItems_TatcBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "TatcBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AssistantMailboxes",
                columns: new[] { "Id", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "ModifiedAt", "ModifiedBy", "Notes", "Topic" },
                values: new object[,]
                {
                    { new Guid("501c69c3-4255-3394-8838-8a916d18bfff"), "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "boservice@hapag-lloyd.com", true, null, null, "Casilla por validar con Customer Service Bolivia antes de producción.", "GENERAL" },
                    { new Guid("b305d4b2-1f96-e2ba-6fbf-3306a86c0f69"), "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "clservice@hapag-lloyd.com", true, null, null, "Casilla de Customer Service publicada en la FAQ del portal.", "GENERAL" }
                });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000001"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "CLSCL", "TelexReleased", new DateTime(2026, 4, 2, 14, 0, 0, 0, DateTimeKind.Utc), "CLSAI", "BL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000002"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "CLVAP", "Issued", new DateTime(2026, 4, 16, 9, 0, 0, 0, DateTimeKind.Utc), "CLVAP", "SWB" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000003"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "CLSCL", "Surrendered", new DateTime(2026, 2, 16, 12, 0, 0, 0, DateTimeKind.Utc), "CLSAI", "BL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000004"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "BOLPB", "AuthorizedAtDestination", new DateTime(2026, 3, 20, 15, 0, 0, 0, DateTimeKind.Utc), "CLARI", "BL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000005"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "BOSRZ", "IssuedAtDestination", new DateTime(2026, 5, 8, 13, 0, 0, 0, DateTimeKind.Utc), "CLIQQ", "BL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000006"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, "WAVE", "NLRTM", "Pending", null, "NLRTM", "EBL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000007"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, "WAVE", "CNSHA", "Issued", new DateTime(2026, 10, 29, 10, 0, 0, 0, DateTimeKind.Utc), "CNSHA", "EBL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000008"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "PELIM", "Pending", null, "PECLL", "SWB" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000009"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "CLSCL", "Issued", new DateTime(2026, 7, 16, 11, 0, 0, 0, DateTimeKind.Utc), "CLSAI", "BL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000010"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "CLSCL", "Issued", new DateTime(2026, 8, 21, 10, 0, 0, 0, DateTimeKind.Utc), "CLSAI", "SWB" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000011"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, "WAVE", "CLVAP", "Transferred", new DateTime(2026, 9, 1, 16, 0, 0, 0, DateTimeKind.Utc), "CLVAP", "EBL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000012"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "CLSCL", "TelexReleased", new DateTime(2026, 9, 30, 18, 0, 0, 0, DateTimeKind.Utc), "CLSAI", "BL" });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000013"),
                columns: new[] { "DifuCode", "DifuLocationCode", "EblPlatform", "FinalDestinationCode", "IssuanceStatus", "IssuanceStatusAt", "PortOfDischargeCode", "TransportDocumentType" },
                values: new object[] { null, null, null, "CLVAP", "Issued", new DateTime(2026, 8, 31, 9, 0, 0, 0, DateTimeKind.Utc), "CLVAP", "BL" });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000014"), "HLCUSAI260601410", null, "HLCUBKG2606141", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "CLANF", 3100m, "USD", null, null, null, false, false, "Issued", new DateTime(2026, 9, 2, 12, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Antofagasta, Chile", "San Antonio (CLSAI)", "CLSAI", "Shanghai (CNSHA)", "Import", "Shanghai Mining Supplies Co.", "Arrived", "BL", "Lima Express", null, "2612E" },
                    { new Guid("11111111-0007-0007-0007-000000000015"), "HLCUSAI260601520", null, "HLCUBKG2606152", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "PUQ-DIFU-0915", "CLPUQ", new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "CLPUQ", 2750m, "USD", new DateTime(2026, 9, 5, 15, 0, 0, 0, DateTimeKind.Utc), null, null, false, false, "Issued", new DateTime(2026, 9, 2, 12, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Punta Arenas, Chile", "San Antonio (CLSAI)", "CLSAI", "Shanghai (CNSHA)", "Import", "Shanghai Cold Chain Ltd", "Arrived", "SWB", "Lima Express", null, "2612E" }
                });

            migrationBuilder.InsertData(
                table: "DangerousGoods",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber" },
                values: new object[,]
                {
                    { new Guid("077e55d6-7b2e-253b-c6c9-c5bd14640d3e"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "8", true, true, "baterias, acumuladores, bateria de plomo", null, null, null, null, "Batteries, wet, filled with acid", "Baterías húmedas llenas de ácido", "un2794 2794 baterias humedas llenas de acido batteries, wet, filled with acid baterias, acumuladores, bateria de plomo", "SAMPLE", null, "2794" },
                    { new Guid("0a52470d-60bc-f9c2-e321-f92766b42e8c"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "3", true, true, "petroleo diesel, gasoil, combustible", null, null, null, "III", "Diesel fuel", "Combustible diésel", "un1202 1202 combustible diesel diesel fuel petroleo diesel, gasoil, combustible", "SAMPLE", null, "1202" },
                    { new Guid("0d4ec6d0-512d-2417-9bf6-7a963350c92f"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "9", true, true, "baterias de litio, pilas de litio", null, null, "Incluye baterías de polímero de ion litio.", null, "Lithium ion batteries", "Baterías de ion litio", "un3480 3480 baterias de ion litio lithium ion batteries baterias de litio, pilas de litio", "SAMPLE", null, "3480" },
                    { new Guid("105d0eac-743d-7d48-d7b7-b13227141e48"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, true, false, "cobre, catodos", null, null, null, null, "Copper cathodes", "Cátodos de cobre", "catodos de cobre copper cathodes cobre, catodos", "SAMPLE", null, null },
                    { new Guid("22362860-9851-a198-09da-7fe8490b5ede"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "5.1", true, true, "nitrato de amonio", null, null, "Con no más del 0,2 % de sustancia combustible.", "III", "Ammonium nitrate", "Nitrato de amonio", "un1942 1942 nitrato de amonio ammonium nitrate nitrato de amonio", "SAMPLE", null, "1942" },
                    { new Guid("24765d35-fb69-b6b5-85f8-ded3bb725df7"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "3", true, true, "bencina, nafta, combustible", null, null, null, "II", "Gasoline (motor spirit)", "Gasolina", "un1203 1203 gasolina gasoline (motor spirit) bencina, nafta, combustible", "SAMPLE", null, "1203" },
                    { new Guid("304efdd2-7458-277b-1c60-2af4af1af337"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "9", true, true, "hielo seco, co2 solido", null, null, null, null, "Carbon dioxide, solid (dry ice)", "Dióxido de carbono sólido (hielo seco)", "un1845 1845 dioxido de carbono solido (hielo seco) carbon dioxide, solid (dry ice) hielo seco, co2 solido", "SAMPLE", null, "1845" },
                    { new Guid("327ab997-3424-cb68-15b0-3b7b33aaf1b8"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "8", true, true, "soda caustica, sosa caustica", null, null, null, "II", "Sodium hydroxide, solid", "Hidróxido de sodio sólido", "un1823 1823 hidroxido de sodio solido sodium hydroxide, solid soda caustica, sosa caustica", "SAMPLE", null, "1823" },
                    { new Guid("336746ef-6353-0852-f1ce-9537ca17110f"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "5.1", true, true, "cloro, cloro granulado, piscina", null, null, null, "II", "Calcium hypochlorite, dry", "Hipoclorito de calcio seco", "un1748 1748 hipoclorito de calcio seco calcium hypochlorite, dry cloro, cloro granulado, piscina", "SAMPLE", null, "1748" },
                    { new Guid("36f15fa3-1970-ac8d-989d-6e23f2d81c61"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "2.1", true, true, "gas propano, glp", null, null, null, null, "Propane", "Propano", "un1978 1978 propano propane gas propano, glp", "SAMPLE", null, "1978" },
                    { new Guid("4c7b6b59-ddd3-95f4-5279-79efa3f44e22"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, true, false, "ropa, textiles, vestuario", null, null, null, null, "Clothing", "Prendas de vestir", "prendas de vestir clothing ropa, textiles, vestuario", "SAMPLE", null, null },
                    { new Guid("50ef7190-04da-4782-2266-529843702934"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, true, false, "vino, bebidas alcoholicas", null, null, "Las bebidas alcohólicas con más de 24 % de alcohol en volumen se clasifican como UN3065, clase 3.", null, "Bottled wine", "Vino embotellado", "vino embotellado bottled wine vino, bebidas alcoholicas", "SAMPLE", null, null },
                    { new Guid("5c5b3d26-bc80-92ac-c4e3-d2ff92ad99e8"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "3", true, true, "liquido inflamable", null, null, "Grupo de embalaje según el punto de inflamación.", null, "Flammable liquid, n.o.s.", "Líquido inflamable, n.e.p.", "un1993 1993 liquido inflamable, n.e.p. flammable liquid, n.o.s. liquido inflamable", "SAMPLE", null, "1993" },
                    { new Guid("61cf63a7-152d-af37-4217-c3a609aa67b4"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "5.1", true, true, "fertilizante, abono", null, null, null, "III", "Ammonium nitrate based fertilizer", "Abonos a base de nitrato de amonio", "un2067 2067 abonos a base de nitrato de amonio ammonium nitrate based fertilizer fertilizante, abono", "SAMPLE", null, "2067" },
                    { new Guid("68b6e81e-140d-38e3-53bf-f395f3607ae5"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "3", true, true, "alcohol etilico, alcohol", null, null, "Grupo de embalaje II o III según la concentración.", "II", "Ethanol (ethyl alcohol) or ethanol solution", "Etanol (alcohol etílico) o solución de etanol", "un1170 1170 etanol (alcohol etilico) o solucion de etanol ethanol (ethyl alcohol) or ethanol solution alcohol etilico, alcohol", "SAMPLE", null, "1170" },
                    { new Guid("6fbed7a0-0f71-dfb5-5bfa-5580ac1cd6d1"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "9", true, true, "baterias de litio, equipos electronicos", null, null, null, null, "Lithium ion batteries contained in equipment or packed with equipment", "Baterías de ion litio contenidas en un equipo o embaladas con él", "un3481 3481 baterias de ion litio contenidas en un equipo o embaladas con el lithium ion batteries contained in equipment or packed with equipment baterias de litio, equipos electronicos", "SAMPLE", null, "3481" },
                    { new Guid("7e4bfe6c-34af-523c-57a2-074bbda12a84"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "9", true, true, "contaminante marino", null, null, null, "III", "Environmentally hazardous substance, solid, n.o.s.", "Sustancia sólida peligrosa para el medio ambiente, n.e.p.", "un3077 3077 sustancia solida peligrosa para el medio ambiente, n.e.p. environmentally hazardous substance, solid, n.o.s. contaminante marino", "SAMPLE", null, "3077" },
                    { new Guid("8618626d-f8e5-680d-b35a-eea8b98e93a0"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "2.3", true, true, "amoniaco, refrigerante", null, null, null, null, "Ammonia, anhydrous", "Amoníaco anhidro", "un1005 1005 amoniaco anhidro ammonia, anhydrous amoniaco, refrigerante", "SAMPLE", "8", "1005" },
                    { new Guid("9df6d733-49f7-4989-63a0-ff8d601424fc"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "2.1", true, true, "glp, gas licuado", null, null, null, null, "Petroleum gases, liquefied", "Gases de petróleo licuados", "un1075 1075 gases de petroleo licuados petroleum gases, liquefied glp, gas licuado", "SAMPLE", null, "1075" },
                    { new Guid("a32273dd-ab7d-00e5-6006-7e8c91bf8797"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "8", true, true, "acido sulfurico", null, null, null, "II", "Sulphuric acid", "Ácido sulfúrico", "un1830 1830 acido sulfurico sulphuric acid acido sulfurico", "SAMPLE", null, "1830" },
                    { new Guid("ac55b5f8-ff9c-c7da-9364-3ea41d040a6b"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "4.2", true, true, "carbon vegetal, carbon", null, null, "Grupo de embalaje II o III.", null, "Carbon, animal or vegetable origin", "Carbón de origen animal o vegetal", "un1361 1361 carbon de origen animal o vegetal carbon, animal or vegetable origin carbon vegetal, carbon", "SAMPLE", null, "1361" },
                    { new Guid("bffa68f3-de6e-6f8d-a472-ca8dce2a9e1c"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, true, false, "muebles, mobiliario", null, null, null, null, "Wooden furniture", "Muebles de madera", "muebles de madera wooden furniture muebles, mobiliario", "SAMPLE", null, null },
                    { new Guid("c109a092-3cf7-5b9a-1bda-25476a9ed4a8"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "3", true, true, "pinturas, barniz, laca, esmalte", null, null, "Grupo de embalaje I, II o III según el punto de inflamación.", null, "Paint", "Pintura", "un1263 1263 pintura paint pinturas, barniz, laca, esmalte", "SAMPLE", null, "1263" },
                    { new Guid("c2f40342-cc39-4603-7153-f475053adf2b"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "9", true, true, "baterias de litio, pilas de litio", null, null, null, null, "Lithium metal batteries", "Baterías de metal litio", "un3090 3090 baterias de metal litio lithium metal batteries baterias de litio, pilas de litio", "SAMPLE", null, "3090" },
                    { new Guid("c3941ce9-d357-2e6c-351c-388924c9a455"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, true, false, "fruta, manzanas, uvas, cerezas", null, null, "Carga refrigerada no clasificada como mercancía peligrosa.", null, "Fresh fruit", "Fruta fresca", "fruta fresca fresh fruit fruta, manzanas, uvas, cerezas", "SAMPLE", null, null },
                    { new Guid("d19fd508-6e73-b57b-4130-d86abe076cc4"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "3", true, true, "alcohol", null, null, "Grupo de embalaje II o III según el punto de inflamación.", null, "Alcohols, n.o.s.", "Alcoholes, n.e.p.", "un1987 1987 alcoholes, n.e.p. alcohols, n.o.s. alcohol", "SAMPLE", null, "1987" },
                    { new Guid("d301d0ab-c9c3-abc7-8615-4eff72143cc1"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "9", true, true, "automovil, auto, vehiculo", null, null, null, null, "Vehicle, flammable liquid powered", "Vehículo propulsado por líquido inflamable", "un3166 3166 vehiculo propulsado por liquido inflamable vehicle, flammable liquid powered automovil, auto, vehiculo", "SAMPLE", null, "3166" },
                    { new Guid("d81659f8-e496-ef4a-5d69-04669899a616"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "2.1", true, true, "spray, aerosol", null, null, "La división (2.1, 2.2 o 2.3) depende del contenido del aerosol.", null, "Aerosols", "Aerosoles", "un1950 1950 aerosoles aerosols spray, aerosol", "SAMPLE", null, "1950" },
                    { new Guid("d9bab528-d5bd-3ca1-e223-ae7685f250af"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "9", true, true, "contaminante marino", null, null, null, "III", "Environmentally hazardous substance, liquid, n.o.s.", "Sustancia líquida peligrosa para el medio ambiente, n.e.p.", "un3082 3082 sustancia liquida peligrosa para el medio ambiente, n.e.p. environmentally hazardous substance, liquid, n.o.s. contaminante marino", "SAMPLE", null, "3082" },
                    { new Guid("e1710ee1-b144-16d0-595a-d4094795db40"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "2.2", true, true, "co2, gas carbonico", null, null, null, null, "Carbon dioxide", "Dióxido de carbono", "un1013 1013 dioxido de carbono carbon dioxide co2, gas carbonico", "SAMPLE", null, "1013" },
                    { new Guid("ec432f34-8f5a-b0ea-8990-8c6e2dbf415a"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "8", true, true, "acido clorhidrico, acido muriatico", null, null, "Grupo de embalaje II o III según la concentración.", null, "Hydrochloric acid", "Ácido clorhídrico", "un1789 1789 acido clorhidrico hydrochloric acid acido clorhidrico, acido muriatico", "SAMPLE", null, "1789" },
                    { new Guid("ecb4a1d5-2dab-226d-7bdc-8244a9635258"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "3", true, true, "solvente, quitaesmalte", null, null, null, "II", "Acetone", "Acetona", "un1090 1090 acetona acetone solvente, quitaesmalte", "SAMPLE", null, "1090" }
                });

            migrationBuilder.InsertData(
                table: "KnowledgeArticles",
                columns: new[] { "Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic" },
                values: new object[,]
                {
                    { new Guid("0cf519fe-d8a2-241f-740d-011e9f6eec9d"), "El NIT (Número de Identificación Tributaria) es el identificador fiscal en Bolivia. Es obligatorio para el registro en el portal y para la emisión de documentos fiscales.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 4, new Guid("f6a7b8c9-0006-0006-0006-000000000014"), "¿Qué es el NIT y por qué lo necesito?", "GENERAL" },
                    { new Guid("16a9eb6a-7ade-d4a6-e84b-1f7b78787229"), "En Bolivia puede pagar mediante Transferencia Bancaria, Efectivo y Cheque. Los pagos en efectivo deben realizarse en oficinas autorizadas.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 2, new Guid("f6a7b8c9-0006-0006-0006-000000000012"), "¿Qué métodos de pago están disponibles en Bolivia?", "PAYMENTS" },
                    { new Guid("1d1add93-0f39-af1b-5416-c59e1ae23e74"), "En Accesos de terceros, el administrador de su organización puede otorgar acceso a un BL o booking, de forma individual o masiva, con vigencia y permisos definidos, y revocarlo cuando quiera. También puede configurar terceros por defecto para los BL nuevos. Todos los cambios quedan registrados en la auditoría.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "acceso, terceros, agencia, mandato, otorgar, revocar", null, null, 26, null, "¿Cómo doy acceso a mi agencia de aduanas u otro tercero?", "GENERAL" },
                    { new Guid("1d9a519c-78d6-5e51-dff3-20d2aeeea4ab"), "Agregue al carro los cargos pendientes desde el detalle del BL, la pestaña de demurrage o sus facturas, indicando el RUT de facturación. El carro agrupa los ítems por país y moneda de pago; cada grupo se paga por separado con los medios habilitados, por ejemplo Khipu, botón de pago bancario o depósito con boleta.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "carro, pago, pagar, khipu, deposito, boleta, moneda, rut de facturacion", null, null, 25, null, "¿Cómo pago mis servicios en el carro?", "PAYMENTS" },
                    { new Guid("2e485c6e-0932-aa83-fca0-44c221257b56"), "En Documentos del embarque solicite el certificado de transbordo: el portal agrega el cargo del servicio según la tarifa vigente y, una vez confirmado el pago en el carro, emite el certificado firmado, lo publica en el repositorio del BL y lo envía por correo.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "certificado, transbordo, certificado de transbordo", null, null, 23, null, "¿Cómo obtengo el certificado de transbordo?", "DOCUMENTATION" },
                    { new Guid("3746d3f8-f379-04cc-942e-236c3c248994"), "El demurrage se calcula desde la fecha de descarga en el puerto chileno. Los días libres y tarifas diarias dependen del tipo de contenedor y acuerdos comerciales. Puede solicitar exenciones a través del módulo de Demurrage.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 5, new Guid("f6a7b8c9-0006-0006-0006-000000000015"), "¿Cómo funciona el demurrage para carga en tránsito a Bolivia?", "DEMURRAGE" },
                    { new Guid("38e0bbdf-dcfd-ad4b-7425-a43db4786451"), "Puede contactarnos al correo clservice@hapag-lloyd.com o llamar al +56 2 2630 1700 (Chile) / +591 2 211 0700 (Bolivia) en horario de oficina de lunes a viernes.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 12, new Guid("f6a7b8c9-0006-0006-0006-000000000023"), "¿Cómo contacto a soporte técnico?", "GENERAL" },
                    { new Guid("39b01132-be66-8672-471e-d76ffb03210f"), "Ingrese al módulo 'Bills of Lading' y busque por número de BL. Verá el estado de su carga incluyendo el puerto de ingreso (Arica, Iquique o Antofagasta) y los cargos asociados.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 1, new Guid("f6a7b8c9-0006-0006-0006-000000000011"), "¿Cómo puedo consultar el estado de mi BL en Bolivia?", "SHIPPING" },
                    { new Guid("4e576670-dc83-d46f-b8df-48b2cce4003e"), "Desde el detalle del BL o la sección Cambio de almacén puede solicitar el cambio para el BL completo o para un contenedor. Si su cuenta tiene derecho a un cambio gratuito (condición informada por Nexus o regla del portal), la solicitud queda completada sin costo. Si no, se aplica la tarifa vigente (KTE o KTF) y el cargo queda listo para pagarlo en el carro. Para varios BL use la solicitud masiva y consulte su avance en la misma sección.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "almacen, cambio de almacen, bodega, KTE, KTF, deposito", null, null, 20, null, "¿Cómo funciona el cambio de almacén?", "SHIPPING" },
                    { new Guid("55710394-2ba1-ea8b-e512-20c30b428663"), "Si su organización es un Freight Forwarder autorizado y es consignatario del BL, debe emitir la carta de responsabilidad antes de pagar los cargos del embarque. Se genera en Documentos del embarque completando los datos del firmante y aceptando los términos vigentes; una carta vigente levanta el bloqueo de ese BL.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "carta, carta de responsabilidad, ffww, freight forwarder, bloqueo", null, null, 22, null, "¿Qué es la carta de responsabilidad para Freight Forwarders?", "DOCUMENTATION" },
                    { new Guid("56ae180b-f99c-6a17-1eea-6e0c979bec7f"), "Algunas cuentas de Bolivia deben pagar demoras anticipadas por contenedor antes de emitir el CLD. El portal las informa en la pestaña de demurrage del BL; al pagarlas, el monto se descuenta del MHD y deja de bloquear el CLD.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "demoras anticipadas, adelanto, demurrage, mhd", null, null, 21, null, "¿Qué son las demoras anticipadas?", "DEMURRAGE" },
                    { new Guid("5a58c5dd-046f-3f66-b69b-2b680c1c146d"), "Vaya al módulo 'Cambio de Almacén', ingrese el número de BL, el contenedor, el almacén actual y el almacén destino. La solicitud será procesada y recibirá confirmación por correo.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 3, new Guid("f6a7b8c9-0006-0006-0006-000000000003"), "¿Cómo solicito un cambio de almacén?", "SHIPPING" },
                    { new Guid("5a6f0183-6ea9-4ca5-4022-a73b23a80b89"), "En Documentos del embarque de un BL de importación de Bolivia consulte si el CLD está disponible. Se emite firmado cuando no hay recargos, demurrage, facturas, flete Collect ni demoras anticipadas pendientes; si algo falta, el portal indica qué bloquea la emisión.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "cld, libre deuda, certificado, bolivia", null, null, 20, null, "¿Cómo obtengo el certificado de libre deuda (CLD)?", "DOCUMENTATION" },
                    { new Guid("5dfa074c-9654-883f-68a8-2dafe0784949"), "La tasa de IVA vigente en Bolivia es del 13%. Se aplica automáticamente sobre los cargos locales y servicios facturables. Los montos se manejan en Bolivianos (BOB).", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 3, new Guid("f6a7b8c9-0006-0006-0006-000000000013"), "¿Cuál es la tasa de IVA aplicada en Bolivia?", "PAYMENTS" },
                    { new Guid("66411fca-e5ab-f656-ff6d-a7a9f60ed389"), "En la pantalla de login, haga clic en '¿Olvidó su contraseña?'. Ingrese su correo electrónico y recibirá un enlace para restablecer su contraseña.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 11, new Guid("f6a7b8c9-0006-0006-0006-000000000022"), "¿Olvidé mi contraseña, cómo la recupero?", "GENERAL" },
                    { new Guid("715a32fa-66b0-77ea-386c-de7cd891089f"), "En Chile puede pagar con Tarjeta de Crédito, Tarjeta de Débito, Transferencia Bancaria y WebPay. Todos los pagos electrónicos se procesan en tiempo real.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 2, new Guid("f6a7b8c9-0006-0006-0006-000000000002"), "¿Qué métodos de pago están disponibles en Chile?", "PAYMENTS" },
                    { new Guid("96bd2015-d692-1d1d-3b1d-e217f757b716"), "Ingrese al módulo 'Bills of Lading', escriba su número de BL en el buscador y presione buscar. Verá el detalle completo incluyendo contenedores, cargos locales y demurrage.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 1, new Guid("f6a7b8c9-0006-0006-0006-000000000001"), "¿Cómo puedo consultar el estado de mi BL?", "SHIPPING" },
                    { new Guid("9a6bc66a-dfa0-63b8-a053-f9d11bdf40c9"), "En el carro elija Depósito o transferencia bancaria y emita la boleta. Realice el depósito o la transferencia por el monto indicado; Finanzas confirma el abono y el pago queda confirmado en el historial de pagos.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "deposito, transferencia, boleta, pago, bolivianos", null, null, 22, null, "¿Cómo pago con depósito o transferencia en Bolivia?", "PAYMENTS" },
                    { new Guid("a43ec294-e615-b557-7514-1d74754dc32b"), "Una vez confirmado el pago, vaya al detalle del pago y presione 'Generar Recibo'. El recibo se genera automáticamente en formato PDF con todos los datos fiscales.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 5, new Guid("f6a7b8c9-0006-0006-0006-000000000005"), "¿Cómo genero un recibo de pago?", "DOCUMENTATION" },
                    { new Guid("a5db2fd7-be42-da7c-64c0-510c8a1da34b"), "En Documentos del embarque puede solicitar la copia del BL valorada (con fletes y cargos) o no valorada (sin valores comerciales). La copia se publica en el repositorio del embarque y se envía al correo registrado de su organización. El shipper solo accede a la copia no valorada.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "copia, copia de bl, valorada, no valorada, documento", null, null, 21, null, "¿Cómo solicito una copia del BL?", "DOCUMENTATION" },
                    { new Guid("a7624e2a-707b-e991-f46a-7e54449dd785"), "En Accesos de terceros, el administrador de su organización puede otorgar acceso a un BL o booking, con vigencia y permisos definidos, y revocarlo cuando quiera. Todos los cambios quedan registrados en la auditoría.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "acceso, terceros, agencia, mandato, otorgar, revocar", null, null, 24, null, "¿Cómo doy acceso a mi agencia de aduanas u otro tercero?", "GENERAL" },
                    { new Guid("cb0c73ee-3a8a-5b3b-9033-b8087818da70"), "La tasa de IVA vigente en Chile es del 19%. Se aplica automáticamente sobre los cargos locales y servicios facturables.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 4, new Guid("f6a7b8c9-0006-0006-0006-000000000004"), "¿Cuál es la tasa de IVA aplicada en Chile?", "PAYMENTS" },
                    { new Guid("d1273909-b058-56f3-cd79-8b966b84fe8b"), "En el detalle de un BL de importación, la sección TATC muestra el estado vigente de cada contenedor según el sistema de TATC (sin emitir, pre-TATC, emitido o anulado) y los motivos pendientes, como pagos o documentos. Los clientes con alto volumen en una misma localidad pueden solicitar la generación masiva de TATC.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "tatc, retiro, contenedor, generacion masiva", null, null, 24, null, "¿Dónde consulto el estado del TATC?", "DOCUMENTATION" },
                    { new Guid("dfbd1162-e8fd-569d-96bf-dbd1963d5fa1"), "En el detalle de un BL de importación, la sección TATC muestra el estado vigente de cada contenedor según el sistema de TATC y los motivos pendientes. Para operaciones de alto volumen en una misma localidad puede solicitar la generación masiva de TATC.", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, "tatc, retiro, contenedor, generacion masiva", null, null, 23, null, "¿Dónde consulto el estado del TATC?", "DOCUMENTATION" },
                    { new Guid("f569fc25-d919-0336-5d34-38d94c4bfa4f"), "Haga clic en 'Registrarse', seleccione su país (Chile o Bolivia), ingrese los datos de su empresa (RUT/NIT, nombre, correo) y cree una contraseña. Recibirá un correo de confirmación.", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, null, 10, new Guid("f6a7b8c9-0006-0006-0006-000000000021"), "¿Cómo registro mi empresa en el portal?", "GENERAL" }
                });

            migrationBuilder.InsertData(
                table: "MaintainerChangeLogs",
                columns: new[] { "Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue" },
                values: new object[,]
                {
                    { new Guid("04ebfebf-4705-16f2-4f2e-eb85300cf792"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("cb0c73ee-3a8a-5b3b-9033-b8087818da70"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"PAYMENTS\",\"title\":\"\\u00BFCu\\u00E1l es la tasa de IVA aplicada en Chile?\",\"content\":\"La tasa de IVA vigente en Chile es del 19%. Se aplica autom\\u00E1ticamente sobre los cargos locales y servicios facturables.\",\"keywords\":null,\"sortOrder\":4,\"isActive\":true}", null },
                    { new Guid("0c9f6b00-f9a6-debc-c14a-7e1f21d25b09"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("16a9eb6a-7ade-d4a6-e84b-1f7b78787229"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"PAYMENTS\",\"title\":\"\\u00BFQu\\u00E9 m\\u00E9todos de pago est\\u00E1n disponibles en Bolivia?\",\"content\":\"En Bolivia puede pagar mediante Transferencia Bancaria, Efectivo y Cheque. Los pagos en efectivo deben realizarse en oficinas autorizadas.\",\"keywords\":null,\"sortOrder\":2,\"isActive\":true}", null },
                    { new Guid("0ee73058-562b-0d9c-5f40-f1d916eb621d"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("5a58c5dd-046f-3f66-b69b-2b680c1c146d"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"SHIPPING\",\"title\":\"\\u00BFC\\u00F3mo solicito un cambio de almac\\u00E9n?\",\"content\":\"Vaya al m\\u00F3dulo \\u0027Cambio de Almac\\u00E9n\\u0027, ingrese el n\\u00FAmero de BL, el contenedor, el almac\\u00E9n actual y el almac\\u00E9n destino. La solicitud ser\\u00E1 procesada y recibir\\u00E1 confirmaci\\u00F3n por correo.\",\"keywords\":null,\"sortOrder\":3,\"isActive\":true}", null },
                    { new Guid("1758de8e-db7b-f140-0035-1b74fa2dcc71"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("39b01132-be66-8672-471e-d76ffb03210f"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"SHIPPING\",\"title\":\"\\u00BFC\\u00F3mo puedo consultar el estado de mi BL en Bolivia?\",\"content\":\"Ingrese al m\\u00F3dulo \\u0027Bills of Lading\\u0027 y busque por n\\u00FAmero de BL. Ver\\u00E1 el estado de su carga incluyendo el puerto de ingreso (Arica, Iquique o Antofagasta) y los cargos asociados.\",\"keywords\":null,\"sortOrder\":1,\"isActive\":true}", null },
                    { new Guid("1c9945f0-fef8-6143-5cac-5262b6360710"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("66411fca-e5ab-f656-ff6d-a7a9f60ed389"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"GENERAL\",\"title\":\"\\u00BFOlvid\\u00E9 mi contrase\\u00F1a, c\\u00F3mo la recupero?\",\"content\":\"En la pantalla de login, haga clic en \\u0027\\u00BFOlvid\\u00F3 su contrase\\u00F1a?\\u0027. Ingrese su correo electr\\u00F3nico y recibir\\u00E1 un enlace para restablecer su contrase\\u00F1a.\",\"keywords\":null,\"sortOrder\":11,\"isActive\":true}", null },
                    { new Guid("23016027-cc30-1ce9-7dce-0d4b58bb031c"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("dfbd1162-e8fd-569d-96bf-dbd1963d5fa1"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"DOCUMENTATION\",\"title\":\"\\u00BFD\\u00F3nde consulto el estado del TATC?\",\"content\":\"En el detalle de un BL de importaci\\u00F3n, la secci\\u00F3n TATC muestra el estado vigente de cada contenedor seg\\u00FAn el sistema de TATC y los motivos pendientes. Para operaciones de alto volumen en una misma localidad puede solicitar la generaci\\u00F3n masiva de TATC.\",\"keywords\":\"tatc, retiro, contenedor, generacion masiva\",\"sortOrder\":23,\"isActive\":true}", null },
                    { new Guid("2e984f5f-ce4d-8337-cec7-b6598ed60abc"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("38e0bbdf-dcfd-ad4b-7425-a43db4786451"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"GENERAL\",\"title\":\"\\u00BFC\\u00F3mo contacto a soporte t\\u00E9cnico?\",\"content\":\"Puede contactarnos al correo clservice@hapag-lloyd.com o llamar al \\u002B56 2 2630 1700 (Chile) / \\u002B591 2 211 0700 (Bolivia) en horario de oficina de lunes a viernes.\",\"keywords\":null,\"sortOrder\":12,\"isActive\":true}", null },
                    { new Guid("330b0267-314d-b97e-6b7e-be7e77469a3a"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0019-0019-0019-000000000001"), "ShipmentPublicationRule", "{\"country\":\"CL\",\"finalDestinationCode\":\"CLANF\",\"finalDestinationName\":\"Antofagasta\",\"dischargePortCode\":\"CLSAI\",\"description\":\"Carga con destino final Antofagasta distribuida desde San Antonio: se publica con DIFU asociado al destino final.\",\"isActive\":true}", null },
                    { new Guid("41f0ba56-f20c-64d3-e1be-03d34ada591a"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("2e485c6e-0932-aa83-fca0-44c221257b56"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"DOCUMENTATION\",\"title\":\"\\u00BFC\\u00F3mo obtengo el certificado de transbordo?\",\"content\":\"En Documentos del embarque solicite el certificado de transbordo: el portal agrega el cargo del servicio seg\\u00FAn la tarifa vigente y, una vez confirmado el pago en el carro, emite el certificado firmado, lo publica en el repositorio del BL y lo env\\u00EDa por correo.\",\"keywords\":\"certificado, transbordo, certificado de transbordo\",\"sortOrder\":23,\"isActive\":true}", null },
                    { new Guid("448be100-c31a-072d-a990-dd783e9be688"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("55710394-2ba1-ea8b-e512-20c30b428663"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"DOCUMENTATION\",\"title\":\"\\u00BFQu\\u00E9 es la carta de responsabilidad para Freight Forwarders?\",\"content\":\"Si su organizaci\\u00F3n es un Freight Forwarder autorizado y es consignatario del BL, debe emitir la carta de responsabilidad antes de pagar los cargos del embarque. Se genera en Documentos del embarque completando los datos del firmante y aceptando los t\\u00E9rminos vigentes; una carta vigente levanta el bloqueo de ese BL.\",\"keywords\":\"carta, carta de responsabilidad, ffww, freight forwarder, bloqueo\",\"sortOrder\":22,\"isActive\":true}", null },
                    { new Guid("448e2614-c473-a342-7c89-45d7076f9c91"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0019-0019-0019-000000000002"), "ShipmentPublicationRule", "{\"country\":\"CL\",\"finalDestinationCode\":\"CLPUQ\",\"finalDestinationName\":\"Punta Arenas\",\"dischargePortCode\":\"CLSAI\",\"description\":\"Carga con destino final Punta Arenas distribuida desde San Antonio: se publica con DIFU asociado al destino final.\",\"isActive\":true}", null },
                    { new Guid("4580513a-30ca-9b89-9170-3d00fa4974a5"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("9a6bc66a-dfa0-63b8-a053-f9d11bdf40c9"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"PAYMENTS\",\"title\":\"\\u00BFC\\u00F3mo pago con dep\\u00F3sito o transferencia en Bolivia?\",\"content\":\"En el carro elija Dep\\u00F3sito o transferencia bancaria y emita la boleta. Realice el dep\\u00F3sito o la transferencia por el monto indicado; Finanzas confirma el abono y el pago queda confirmado en el historial de pagos.\",\"keywords\":\"deposito, transferencia, boleta, pago, bolivianos\",\"sortOrder\":22,\"isActive\":true}", null },
                    { new Guid("4cf6e84a-b709-eb08-ebb4-fd0ad3591bc5"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("d1273909-b058-56f3-cd79-8b966b84fe8b"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"DOCUMENTATION\",\"title\":\"\\u00BFD\\u00F3nde consulto el estado del TATC?\",\"content\":\"En el detalle de un BL de importaci\\u00F3n, la secci\\u00F3n TATC muestra el estado vigente de cada contenedor seg\\u00FAn el sistema de TATC (sin emitir, pre-TATC, emitido o anulado) y los motivos pendientes, como pagos o documentos. Los clientes con alto volumen en una misma localidad pueden solicitar la generaci\\u00F3n masiva de TATC.\",\"keywords\":\"tatc, retiro, contenedor, generacion masiva\",\"sortOrder\":24,\"isActive\":true}", null },
                    { new Guid("5c8b4814-fc1f-ecfb-c955-ba36a27021dd"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("0cf519fe-d8a2-241f-740d-011e9f6eec9d"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"GENERAL\",\"title\":\"\\u00BFQu\\u00E9 es el NIT y por qu\\u00E9 lo necesito?\",\"content\":\"El NIT (N\\u00FAmero de Identificaci\\u00F3n Tributaria) es el identificador fiscal en Bolivia. Es obligatorio para el registro en el portal y para la emisi\\u00F3n de documentos fiscales.\",\"keywords\":null,\"sortOrder\":4,\"isActive\":true}", null },
                    { new Guid("860c6994-8ce6-781b-0381-b1a608ddf07a"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("5dfa074c-9654-883f-68a8-2dafe0784949"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"PAYMENTS\",\"title\":\"\\u00BFCu\\u00E1l es la tasa de IVA aplicada en Bolivia?\",\"content\":\"La tasa de IVA vigente en Bolivia es del 13%. Se aplica autom\\u00E1ticamente sobre los cargos locales y servicios facturables. Los montos se manejan en Bolivianos (BOB).\",\"keywords\":null,\"sortOrder\":3,\"isActive\":true}", null },
                    { new Guid("8f53822a-0dbd-8b5f-ca47-f7f1bcfd33f8"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("b305d4b2-1f96-e2ba-6fbf-3306a86c0f69"), "AssistantMailbox", "{\"country\":\"CL\",\"topic\":\"GENERAL\",\"email\":\"clservice@hapag-lloyd.com\",\"notes\":\"Casilla de Customer Service publicada en la FAQ del portal.\",\"isActive\":true}", null },
                    { new Guid("912a37d4-1d25-ba26-c523-601cfbb80a4e"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("a5db2fd7-be42-da7c-64c0-510c8a1da34b"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"DOCUMENTATION\",\"title\":\"\\u00BFC\\u00F3mo solicito una copia del BL?\",\"content\":\"En Documentos del embarque puede solicitar la copia del BL valorada (con fletes y cargos) o no valorada (sin valores comerciales). La copia se publica en el repositorio del embarque y se env\\u00EDa al correo registrado de su organizaci\\u00F3n. El shipper solo accede a la copia no valorada.\",\"keywords\":\"copia, copia de bl, valorada, no valorada, documento\",\"sortOrder\":21,\"isActive\":true}", null },
                    { new Guid("9bfaef86-18ff-7fb5-d963-eb8684f03928"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("5a6f0183-6ea9-4ca5-4022-a73b23a80b89"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"DOCUMENTATION\",\"title\":\"\\u00BFC\\u00F3mo obtengo el certificado de libre deuda (CLD)?\",\"content\":\"En Documentos del embarque de un BL de importaci\\u00F3n de Bolivia consulte si el CLD est\\u00E1 disponible. Se emite firmado cuando no hay recargos, demurrage, facturas, flete Collect ni demoras anticipadas pendientes; si algo falta, el portal indica qu\\u00E9 bloquea la emisi\\u00F3n.\",\"keywords\":\"cld, libre deuda, certificado, bolivia\",\"sortOrder\":20,\"isActive\":true}", null },
                    { new Guid("a00f8b4a-8c81-185e-bc3d-d225ff93bc4a"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("96bd2015-d692-1d1d-3b1d-e217f757b716"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"SHIPPING\",\"title\":\"\\u00BFC\\u00F3mo puedo consultar el estado de mi BL?\",\"content\":\"Ingrese al m\\u00F3dulo \\u0027Bills of Lading\\u0027, escriba su n\\u00FAmero de BL en el buscador y presione buscar. Ver\\u00E1 el detalle completo incluyendo contenedores, cargos locales y demurrage.\",\"keywords\":null,\"sortOrder\":1,\"isActive\":true}", null },
                    { new Guid("aaddddcc-3132-82dd-9038-fd2f1f73f65e"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("4e576670-dc83-d46f-b8df-48b2cce4003e"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"SHIPPING\",\"title\":\"\\u00BFC\\u00F3mo funciona el cambio de almac\\u00E9n?\",\"content\":\"Desde el detalle del BL o la secci\\u00F3n Cambio de almac\\u00E9n puede solicitar el cambio para el BL completo o para un contenedor. Si su cuenta tiene derecho a un cambio gratuito (condici\\u00F3n informada por Nexus o regla del portal), la solicitud queda completada sin costo. Si no, se aplica la tarifa vigente (KTE o KTF) y el cargo queda listo para pagarlo en el carro. Para varios BL use la solicitud masiva y consulte su avance en la misma secci\\u00F3n.\",\"keywords\":\"almacen, cambio de almacen, bodega, KTE, KTF, deposito\",\"sortOrder\":20,\"isActive\":true}", null },
                    { new Guid("ab821fdb-1437-7a98-7607-bdbc1c31a414"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("501c69c3-4255-3394-8838-8a916d18bfff"), "AssistantMailbox", "{\"country\":\"BO\",\"topic\":\"GENERAL\",\"email\":\"boservice@hapag-lloyd.com\",\"notes\":\"Casilla por validar con Customer Service Bolivia antes de producci\\u00F3n.\",\"isActive\":true}", null },
                    { new Guid("b0fe0baa-0238-5953-3336-77be180946a0"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("f569fc25-d919-0336-5d34-38d94c4bfa4f"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"GENERAL\",\"title\":\"\\u00BFC\\u00F3mo registro mi empresa en el portal?\",\"content\":\"Haga clic en \\u0027Registrarse\\u0027, seleccione su pa\\u00EDs (Chile o Bolivia), ingrese los datos de su empresa (RUT/NIT, nombre, correo) y cree una contrase\\u00F1a. Recibir\\u00E1 un correo de confirmaci\\u00F3n.\",\"keywords\":null,\"sortOrder\":10,\"isActive\":true}", null },
                    { new Guid("d4f3fa2e-f62e-3b96-5a26-87f96369f490"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("56ae180b-f99c-6a17-1eea-6e0c979bec7f"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"DEMURRAGE\",\"title\":\"\\u00BFQu\\u00E9 son las demoras anticipadas?\",\"content\":\"Algunas cuentas de Bolivia deben pagar demoras anticipadas por contenedor antes de emitir el CLD. El portal las informa en la pesta\\u00F1a de demurrage del BL; al pagarlas, el monto se descuenta del MHD y deja de bloquear el CLD.\",\"keywords\":\"demoras anticipadas, adelanto, demurrage, mhd\",\"sortOrder\":21,\"isActive\":true}", null },
                    { new Guid("dcaab0be-6d77-28fb-fc18-80d79570ad41"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("1d1add93-0f39-af1b-5416-c59e1ae23e74"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"GENERAL\",\"title\":\"\\u00BFC\\u00F3mo doy acceso a mi agencia de aduanas u otro tercero?\",\"content\":\"En Accesos de terceros, el administrador de su organizaci\\u00F3n puede otorgar acceso a un BL o booking, de forma individual o masiva, con vigencia y permisos definidos, y revocarlo cuando quiera. Tambi\\u00E9n puede configurar terceros por defecto para los BL nuevos. Todos los cambios quedan registrados en la auditor\\u00EDa.\",\"keywords\":\"acceso, terceros, agencia, mandato, otorgar, revocar\",\"sortOrder\":26,\"isActive\":true}", null },
                    { new Guid("e461c099-2caf-9f58-a814-94f9e2ad1730"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("715a32fa-66b0-77ea-386c-de7cd891089f"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"PAYMENTS\",\"title\":\"\\u00BFQu\\u00E9 m\\u00E9todos de pago est\\u00E1n disponibles en Chile?\",\"content\":\"En Chile puede pagar con Tarjeta de Cr\\u00E9dito, Tarjeta de D\\u00E9bito, Transferencia Bancaria y WebPay. Todos los pagos electr\\u00F3nicos se procesan en tiempo real.\",\"keywords\":null,\"sortOrder\":2,\"isActive\":true}", null },
                    { new Guid("ed0a9d06-e01c-dec7-ee9d-1d2e72d2cadb"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("a43ec294-e615-b557-7514-1d74754dc32b"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"DOCUMENTATION\",\"title\":\"\\u00BFC\\u00F3mo genero un recibo de pago?\",\"content\":\"Una vez confirmado el pago, vaya al detalle del pago y presione \\u0027Generar Recibo\\u0027. El recibo se genera autom\\u00E1ticamente en formato PDF con todos los datos fiscales.\",\"keywords\":null,\"sortOrder\":5,\"isActive\":true}", null },
                    { new Guid("f3f5b5e0-556e-b2be-b555-bf4f22b80c12"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("1d9a519c-78d6-5e51-dff3-20d2aeeea4ab"), "KnowledgeArticle", "{\"country\":\"CL\",\"topic\":\"PAYMENTS\",\"title\":\"\\u00BFC\\u00F3mo pago mis servicios en el carro?\",\"content\":\"Agregue al carro los cargos pendientes desde el detalle del BL, la pesta\\u00F1a de demurrage o sus facturas, indicando el RUT de facturaci\\u00F3n. El carro agrupa los \\u00EDtems por pa\\u00EDs y moneda de pago; cada grupo se paga por separado con los medios habilitados, por ejemplo Khipu, bot\\u00F3n de pago bancario o dep\\u00F3sito con boleta.\",\"keywords\":\"carro, pago, pagar, khipu, deposito, boleta, moneda, rut de facturacion\",\"sortOrder\":25,\"isActive\":true}", null },
                    { new Guid("f5691cee-26c9-ad8d-2ade-9c7e2eec8999"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("a7624e2a-707b-e991-f46a-7e54449dd785"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"GENERAL\",\"title\":\"\\u00BFC\\u00F3mo doy acceso a mi agencia de aduanas u otro tercero?\",\"content\":\"En Accesos de terceros, el administrador de su organizaci\\u00F3n puede otorgar acceso a un BL o booking, con vigencia y permisos definidos, y revocarlo cuando quiera. Todos los cambios quedan registrados en la auditor\\u00EDa.\",\"keywords\":\"acceso, terceros, agencia, mandato, otorgar, revocar\",\"sortOrder\":24,\"isActive\":true}", null },
                    { new Guid("f8a7cc50-78dc-f8e9-dfed-fdb8d86560b2"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("3746d3f8-f379-04cc-942e-236c3c248994"), "KnowledgeArticle", "{\"country\":\"BO\",\"topic\":\"DEMURRAGE\",\"title\":\"\\u00BFC\\u00F3mo funciona el demurrage para carga en tr\\u00E1nsito a Bolivia?\",\"content\":\"El demurrage se calcula desde la fecha de descarga en el puerto chileno. Los d\\u00EDas libres y tarifas diarias dependen del tipo de contenedor y acuerdos comerciales. Puede solicitar exenciones a trav\\u00E9s del m\\u00F3dulo de Demurrage.\",\"keywords\":null,\"sortOrder\":5,\"isActive\":true}", null }
                });

            migrationBuilder.InsertData(
                table: "ShipmentPublicationRules",
                columns: new[] { "Id", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "DischargePortCode", "FinalDestinationCode", "FinalDestinationName", "IsActive", "ModifiedAt", "ModifiedBy" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0019-0019-0019-000000000001"), "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Carga con destino final Antofagasta distribuida desde San Antonio: se publica con DIFU asociado al destino final.", "CLSAI", "CLANF", "Antofagasta", true, null, null },
                    { new Guid("ffffffff-0019-0019-0019-000000000002"), "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Carga con destino final Punta Arenas distribuida desde San Antonio: se publica con DIFU asociado al destino final.", "CLSAI", "CLPUQ", "Punta Arenas", true, null, null }
                });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000017"), new Guid("11111111-0007-0007-0007-000000000014"), "HLXU3046001", "40HC", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-046001", "Discharged", null, null, 26300m },
                    { new Guid("22222222-0008-0008-0008-000000000018"), new Guid("11111111-0007-0007-0007-000000000015"), "HLXU3046002", "40RF", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-046002", "Discharged", null, null, 24800m }
                });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[] { new Guid("33333333-0009-0009-0009-000000000026"), 210000m, new Guid("11111111-0007-0007-0007-000000000015"), "THC", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 40RF (San Antonio)", true, null, null, "Pending", 39900m, 19m, 249900m });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[,]
                {
                    { new Guid("35804351-fee3-10ce-ed3a-782a1346abcb"), new Guid("11111111-0007-0007-0007-000000000014"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" },
                    { new Guid("3fd9b388-e81b-e6b6-87b4-17bb9996ac3b"), new Guid("11111111-0007-0007-0007-000000000015"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillsOfLading_Country_FinalDestinationCode",
                table: "BillsOfLading",
                columns: new[] { "Country", "FinalDestinationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantMailboxes_Country_Topic",
                table: "AssistantMailboxes",
                columns: new[] { "Country", "Topic" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssistantMessages_Role_CreatedAt",
                table: "AssistantMessages",
                columns: new[] { "Role", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantMessages_SessionId_Sequence",
                table: "AssistantMessages",
                columns: new[] { "SessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssistantSessions_UserId_StartedAt",
                table: "AssistantSessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DangerousGoods_UnNumber",
                table: "DangerousGoods",
                column: "UnNumber");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_Country_IsActive_Topic",
                table: "KnowledgeArticles",
                columns: new[] { "Country", "IsActive", "Topic" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPublicationRules_Country_FinalDestinationCode_IsAct~",
                table: "ShipmentPublicationRules",
                columns: new[] { "Country", "FinalDestinationCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TatcBatches_ClientId_CreatedAt",
                table: "TatcBatches",
                columns: new[] { "ClientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TatcBatchItems_BatchId_LineNumber",
                table: "TatcBatchItems",
                columns: new[] { "BatchId", "LineNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantMailboxes");

            migrationBuilder.DropTable(
                name: "AssistantMessages");

            migrationBuilder.DropTable(
                name: "DangerousGoods");

            migrationBuilder.DropTable(
                name: "KnowledgeArticles");

            migrationBuilder.DropTable(
                name: "ShipmentPublicationRules");

            migrationBuilder.DropTable(
                name: "TatcBatchItems");

            migrationBuilder.DropTable(
                name: "AssistantSessions");

            migrationBuilder.DropTable(
                name: "TatcBatches");

            migrationBuilder.DropIndex(
                name: "IX_BillsOfLading_Country_FinalDestinationCode",
                table: "BillsOfLading");

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000017"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000018"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000026"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("04ebfebf-4705-16f2-4f2e-eb85300cf792"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("0c9f6b00-f9a6-debc-c14a-7e1f21d25b09"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("0ee73058-562b-0d9c-5f40-f1d916eb621d"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("1758de8e-db7b-f140-0035-1b74fa2dcc71"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("1c9945f0-fef8-6143-5cac-5262b6360710"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("23016027-cc30-1ce9-7dce-0d4b58bb031c"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("2e984f5f-ce4d-8337-cec7-b6598ed60abc"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("330b0267-314d-b97e-6b7e-be7e77469a3a"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("41f0ba56-f20c-64d3-e1be-03d34ada591a"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("448be100-c31a-072d-a990-dd783e9be688"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("448e2614-c473-a342-7c89-45d7076f9c91"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("4580513a-30ca-9b89-9170-3d00fa4974a5"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("4cf6e84a-b709-eb08-ebb4-fd0ad3591bc5"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("5c8b4814-fc1f-ecfb-c955-ba36a27021dd"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("860c6994-8ce6-781b-0381-b1a608ddf07a"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("8f53822a-0dbd-8b5f-ca47-f7f1bcfd33f8"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("912a37d4-1d25-ba26-c523-601cfbb80a4e"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("9bfaef86-18ff-7fb5-d963-eb8684f03928"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("a00f8b4a-8c81-185e-bc3d-d225ff93bc4a"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("aaddddcc-3132-82dd-9038-fd2f1f73f65e"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("ab821fdb-1437-7a98-7607-bdbc1c31a414"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("b0fe0baa-0238-5953-3336-77be180946a0"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("d4f3fa2e-f62e-3b96-5a26-87f96369f490"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("dcaab0be-6d77-28fb-fc18-80d79570ad41"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("e461c099-2caf-9f58-a814-94f9e2ad1730"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("ed0a9d06-e01c-dec7-ee9d-1d2e72d2cadb"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("f3f5b5e0-556e-b2be-b555-bf4f22b80c12"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("f5691cee-26c9-ad8d-2ade-9c7e2eec8999"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("f8a7cc50-78dc-f8e9-dfed-fdb8d86560b2"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("35804351-fee3-10ce-ed3a-782a1346abcb"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("3fd9b388-e81b-e6b6-87b4-17bb9996ac3b"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000014"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000015"));

            migrationBuilder.DropColumn(
                name: "DifuCode",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "DifuLocationCode",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "EblPlatform",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "FinalDestinationCode",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "IssuanceStatus",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "IssuanceStatusAt",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "PortOfDischargeCode",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "TransportDocumentType",
                table: "BillsOfLading");
        }
    }
}
