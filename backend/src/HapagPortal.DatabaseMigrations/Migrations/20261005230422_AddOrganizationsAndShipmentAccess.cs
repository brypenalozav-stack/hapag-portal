using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationsAndShipmentAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ClientId",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "MembershipDecidedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MembershipDecidedBy",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MembershipStatus",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArCheckedAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArCheckedBy",
                table: "Clients",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArReference",
                table: "Clients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchCode",
                table: "Clients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingCountries",
                table: "Clients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrganizationType",
                table: "Clients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Customer");

            migrationBuilder.AddColumn<string>(
                name: "RegistrationStatus",
                table: "Clients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "Clients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidatedAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidatedBy",
                table: "Clients",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            // Organizaciones existentes: siguen operando (aprobadas) y toman el tipo desde ClientType.
            migrationBuilder.Sql("""
                UPDATE "Clients" SET "OrganizationType" = CASE "ClientType"
                    WHEN 'Internal' THEN 'Internal'
                    WHEN 'Agent' THEN 'CustomsAgency'
                    WHEN 'CustomsAgent' THEN 'CustomsAgency'
                    ELSE 'Customer' END;
                """);

            migrationBuilder.AddColumn<string>(
                name: "BookingNumber",
                table: "BillsOfLading",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationDocuments_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_ShipmentActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_ShipmentRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentRoles_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentRoles_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentAccessRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrganizationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentAccessRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentAccessRules_ShipmentActions_ShipmentActionId",
                        column: x => x.ShipmentActionId,
                        principalTable: "ShipmentActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000001"),
                column: "BookingNumber",
                value: "HLCUBKG2501001");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000002"),
                column: "BookingNumber",
                value: "HLCUBKG2502004");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000003"),
                column: "BookingNumber",
                value: "HLCUBKG2503007");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000004"),
                column: "BookingNumber",
                value: "HLCUBKG2601045");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000005"),
                column: "BookingNumber",
                value: "HLCUBKG2602078");

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000006"), "HLCUSAI260300610", null, "HLCUBKG2603061", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Fruit Import BV", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3900m, "USD", null, null, false, false, null, null, null, null, null, "Rotterdam, Netherlands", "Rotterdam (NLRTM)", "San Antonio (CLSAI)", "Export", "Importadora Demo SpA", "Booked", "Valparaiso Express", null, "2610S" },
                    { new Guid("11111111-0007-0007-0007-000000000008"), "HLCUARI260300830", null, "HLCUBKG2603083", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "Andes Foods SAC", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 11, 12, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1450m, "USD", null, null, false, false, null, null, null, null, null, "Lima, Peru", "Callao (PECLL)", "Arica (CLARI)", "Export", "Comercial Altiplano SRL", "Booked", "Antofagasta Express", null, "2612S" }
                });

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000001"),
                columns: new[] { "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "MatchCode", "OperatingCountries", "OrganizationType", "RegistrationStatus", "RejectedAt", "ReviewNotes", "ValidatedAt", "ValidatedBy" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "CL,BO", "Internal", "Approved", null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000010"),
                columns: new[] { "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "MatchCode", "OperatingCountries", "OrganizationType", "RegistrationStatus", "RejectedAt", "ReviewNotes", "ValidatedAt", "ValidatedBy" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "MC100010", "CL,BO", "Customer", "Approved", null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000020"),
                columns: new[] { "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "MatchCode", "OperatingCountries", "OrganizationType", "RegistrationStatus", "RejectedAt", "ReviewNotes", "ValidatedAt", "ValidatedBy" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "MC100020", "BO", "Customer", "Approved", null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000030"),
                columns: new[] { "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "MatchCode", "OperatingCountries", "OrganizationType", "RegistrationStatus", "RejectedAt", "ReviewNotes", "ValidatedAt", "ValidatedBy" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "MC100030", "CL", "CustomsAgency", "Approved", null, null, null, null });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy" },
                values: new object[,]
                {
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000040"), null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Client", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "contacto@pacifictrading.cl", true, true, "MC100040", null, null, "Pacific Trading Co.", "CL", "Customer", null, "Approved", null, null, "77.888.999-0", "RUT", null, null },
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000050"), null, null, null, null, null, null, null, "Client", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "registro@logisticaandina.cl", true, true, null, null, null, "Logística Andina SpA", "CL", "FreightForwarder", "+56 2 2999 1234", "PendingValidation", null, null, "76.555.111-2", "RUT", null, null }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { new Guid("208dfafe-5331-55c8-f20c-1fedbb16b3cb"), "org.users.manage", "org.users.manage" },
                    { new Guid("2477f174-66b3-8275-4ef4-b84902cdbb81"), "shipments.operate", "shipments.operate" },
                    { new Guid("579700e8-1338-3738-63e6-8e8b8b7cad71"), "shipments.view-all", "shipments.view-all" },
                    { new Guid("59a9ccfd-f3a1-90b7-b694-bd9eafb81228"), "organizations.review", "organizations.review" },
                    { new Guid("920581cb-9857-6ad8-3406-9f8f54fd25fc"), "organizations.ar-check", "organizations.ar-check" },
                    { new Guid("c474e6b8-5abc-1ffc-bf2a-eeb14f3dd7a7"), "access-matrix.manage", "access-matrix.manage" },
                    { new Guid("d7f60fcb-88d4-54d3-54fb-d63def5c9613"), "org.requests.approve", "org.requests.approve" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsSystem", "ModifiedAt", "ModifiedBy", "Name" },
                values: new object[,]
                {
                    { new Guid("227e87c5-a8d9-51c6-e0e9-373dc6d2c60b"), "OrgOperator", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Operador de organización" },
                    { new Guid("a084c0fb-db38-005b-3d5f-926ed28ed6df"), "OrgViewer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Consulta de organización" },
                    { new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Administrador de organización" }
                });

            migrationBuilder.InsertData(
                table: "ShipmentActions",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope" },
                values: new object[,]
                {
                    { new Guid("032cc54e-cbbe-5272-cab1-32a3bb291f82"), "Information", "tatc.download", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 80, true, "View", null, null, "Descargar documento TATC", "Shipment" },
                    { new Guid("0334bbd0-ffd8-1995-7db7-fdf3d95d6729"), "Information", "bl-copy-unvalued.request", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 50, true, "Operate", null, null, "Solicitar copia de BL no valorada", "Shipment" },
                    { new Guid("07343dd5-fa12-c8e8-a80f-26f110926989"), "Information", "freight.pay", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 90, true, "Operate", null, null, "Visualizar y pagar montos de flete", "Shipment" },
                    { new Guid("1145e12a-7014-9221-40ff-2d3754ffd29f"), "Administration", "join-requests.approve", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 380, true, "Operate", null, null, "Revisar y aprobar solicitudes de registro a la organización", "Organization" },
                    { new Guid("122ef33e-f8f5-fb84-25cb-5176d10c40dc"), "Information", "release-letter.generate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 120, true, "Operate", null, null, "Generar y descargar carta de liberación y desconsolidado", "Shipment" },
                    { new Guid("160ab007-ea1a-063f-52dd-e10cf1dcd00b"), "Administration", "organization-users.own", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 370, true, "Operate", null, null, "Tener usuarios propios asociados a la organización", "Organization" },
                    { new Guid("19ef0081-eb79-5a7c-e09c-62482731cd52"), "Information", "export-depot.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 240, true, "View", null, null, "Ver depósito asignado en exportación", "Shipment" },
                    { new Guid("1c3dd706-407f-2945-5b68-bea5dfd584e9"), "Administration", "third-party-query.notify", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 320, true, "View", null, null, "Recibir notificación cuando un tercero consulta un BL", "Shipment" },
                    { new Guid("2ac5eecb-38d8-6ec0-6d82-f03f854e976c"), "Information", "import-depot.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 230, true, "View", null, null, "Ver depósito asignado en importación", "Shipment" },
                    { new Guid("3eac8579-97de-40ff-10b5-ba572613f061"), "Information", "shipment.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 10, true, "View", null, null, "Ver listado y detalle de BL o booking", "Shipment" },
                    { new Guid("3ff1f234-1eda-6801-33a1-ce8538693d6e"), "Information", "no-debt-certificate.download", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 70, true, "View", null, null, "Descargar certificado de libre deuda", "Shipment" },
                    { new Guid("4900513d-d3e1-f3e2-5b43-c0e000a389f0"), "Information", "local-charges-mandatory.pay", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 100, true, "Operate", null, null, "Visualizar y pagar recargos locales mandatorios", "Shipment" },
                    { new Guid("557bba04-d0ce-91b0-c765-277b91222bf7"), "Administration", "data-visibility.extend", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 350, true, "Operate", null, null, "Ampliar la visibilidad de un dato del BL a otro rol", "Shipment" },
                    { new Guid("5810f4b9-0e60-a000-5697-53177df94bb9"), "Information", "collect-receipt.download", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 220, true, "View", null, null, "Visualizar y descargar comprobante Collect", "Shipment" },
                    { new Guid("5fa67043-8f48-599c-fded-6ea2b5ca1488"), "Administration", "assistant.use", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 420, true, "View", null, null, "Utilizar el asistente del portal y ver los comunicados", "Organization" },
                    { new Guid("65dfa9ad-25d2-4a79-f36c-410bf77ddddc"), "Administration", "open-access.enable", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 290, true, "Operate", null, null, "Activar el acceso abierto por número de BL", "Shipment" },
                    { new Guid("6678139a-eb5f-24c3-d404-7ffcfbf1dde8"), "Information", "release-requirements.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 20, true, "View", null, null, "Consultar estado de los requisitos de liberación", "Shipment" },
                    { new Guid("72baee95-6d9c-f671-270d-67a5ddbb42e5"), "Administration", "open-access.self-associate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 310, true, "Operate", null, null, "Autoasociarse a un BL consultado con acceso abierto", "Organization" },
                    { new Guid("72c736f3-0830-08da-2ba1-aef0b2ec3e81"), "Administration", "country.select", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 430, true, "View", null, null, "Seleccionar el país de operación y consultar la clasificación DG", "Organization" },
                    { new Guid("741f2950-8b45-5e2d-8065-fe163db727a6"), "Administration", "access-validity.set", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 270, true, "Operate", null, null, "Definir la vigencia de un acceso", "Shipment" },
                    { new Guid("7cc9c738-4673-e0c2-9a01-df7b48b28b7c"), "Information", "account-statement.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 190, true, "View", null, null, "Consultar el estado de cuenta en línea", "Shipment" },
                    { new Guid("81d22eff-dca4-0ac0-bf3a-4f3df458d908"), "Information", "tracking.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 30, true, "View", null, null, "Ver el seguimiento del embarque", "Shipment" },
                    { new Guid("9119a3f4-394d-695c-5267-b7cd696f7ba6"), "Administration", "access.revoke", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 260, true, "Operate", null, null, "Revocar un acceso ya otorgado", "Shipment" },
                    { new Guid("93f658d0-ee65-42f2-a3da-646f54feb2d6"), "Administration", "default-agents.configure", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 280, true, "Operate", null, null, "Configurar agencia de aduanas o transportista por defecto", "Organization" },
                    { new Guid("947b0092-be0a-4670-bf9f-46eda7d14ab0"), "Administration", "distribution-list.update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 400, true, "Operate", null, null, "Actualizar la lista de distribución de correos propia", "Organization" },
                    { new Guid("95d43dff-917c-dd4d-eeb5-c49731913c7c"), "Administration", "open-access.search", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 300, true, "View", null, null, "Buscar un BL con acceso abierto por su número", "Organization" },
                    { new Guid("a155eb78-8542-4978-a8dd-bd2b28929738"), "Administration", "parent-company-visibility.enable", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 340, true, "Operate", null, null, "Habilitar la visibilidad de los BL hacia la empresa matriz", "Organization" },
                    { new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332"), "Administration", "early-booking-access.grant", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 360, true, "Operate", null, null, "Otorgar acceso anticipado por booking a un futuro shipper", "Shipment" },
                    { new Guid("a6e130a7-a1ea-abd1-aa46-e6b5c5b63391"), "Information", "bl-issuance.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 40, true, "View", null, null, "Consultar el estado de emisión del BL", "Shipment" },
                    { new Guid("ac5533f1-b185-fe85-07a0-89a89adf382d"), "Information", "invoices-billed.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 200, true, "View", null, null, "Ver facturas como cliente facturado", "Shipment" },
                    { new Guid("bd6cab83-dabc-1619-6d2c-e324cd7d01a1"), "Administration", "carrier.pre-create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 390, true, "Operate", null, null, "Pre-crear el perfil de un transportista sin cuenta", "Organization" },
                    { new Guid("bdcf5a98-4a99-386e-fe2f-1da80f99aa1e"), "Information", "invoices-payer.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 210, true, "View", null, null, "Ver facturas como pagador distinto del facturado", "Shipment" },
                    { new Guid("c8157abe-83ae-64bb-ddbb-b4681e99c33f"), "Information", "drop-off.request", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 170, true, "Operate", null, null, "Solicitar y pagar Drop Off", "Shipment" },
                    { new Guid("ca92d241-6346-1541-212a-e1403a95cb9f"), "Information", "transshipment-certificate.generate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 130, true, "Operate", null, null, "Generar y descargar certificado de transbordo", "Shipment" },
                    { new Guid("cbecbaf4-b71c-43d6-aae2-c344d6bf19ad"), "Information", "local-charges-on-demand.pay", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 110, true, "Operate", null, null, "Visualizar y pagar recargos locales on demand", "Shipment" },
                    { new Guid("cc4c1544-f4f5-8f00-9bb1-8241a64e8d54"), "Information", "freight-certificate.generate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 140, true, "Operate", null, null, "Generar y descargar certificado de flete", "Shipment" },
                    { new Guid("cc502303-816b-de21-d681-447330a0e8c9"), "Information", "responsibility-letter.generate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 150, true, "Operate", null, null, "Generar y descargar carta de responsabilidad", "Shipment" },
                    { new Guid("cfd10f40-3f50-8ebf-fed3-ec080f6c29b5"), "Information", "bl-copy-valued.request", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 60, true, "Operate", null, null, "Solicitar copia de BL valorada", "Shipment" },
                    { new Guid("e112508b-beea-c895-9061-da03cb427883"), "Administration", "access.grant", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 250, true, "Operate", null, null, "Otorgar acceso a un BL o booking, individual o masivo", "Shipment" },
                    { new Guid("e7eb07a5-bc20-230e-0e88-997310452f02"), "Information", "import-demurrage.pay", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 160, true, "Operate", null, null, "Consultar y pagar demurrage de importación", "Shipment" },
                    { new Guid("ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0"), "Administration", "access-audit.view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 330, true, "View", null, null, "Consultar la auditoría de accesos de un BL o booking", "Shipment" },
                    { new Guid("f047ab5d-f553-96e9-9003-6fe15a0a5114"), "Administration", "administration-area.access", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 410, true, "Operate", null, null, "Acceder al área de administración del portal", "Organization" },
                    { new Guid("fe2d4e42-d3b0-aac5-9026-5f918c61aca6"), "Information", "warehouse-change.request", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 180, true, "Operate", null, null, "Solicitar cambio de almacén, individual o masivo", "Shipment" }
                });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[,]
                {
                    { new Guid("30d59ecf-65b1-c621-f6ed-1efcf3d5b75d"), new Guid("11111111-0007-0007-0007-000000000004"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" },
                    { new Guid("49b4cece-2a9e-c46e-4156-6fc62587c5c5"), new Guid("11111111-0007-0007-0007-000000000002"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" },
                    { new Guid("79bda1ef-7050-21dd-b714-f13f9efba29d"), new Guid("11111111-0007-0007-0007-000000000005"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" },
                    { new Guid("b424feb8-e445-a6b5-5dd1-001214133c6a"), new Guid("11111111-0007-0007-0007-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000001"),
                columns: new[] { "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus" },
                values: new object[] { null, null, "Active" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000010"),
                columns: new[] { "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus" },
                values: new object[] { null, null, "Active" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000020"),
                columns: new[] { "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus" },
                values: new object[] { null, null, "Active" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000030"),
                columns: new[] { "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus" },
                values: new object[] { null, null, "Active" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username" },
                values: new object[,]
                {
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000011"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "consulta@importadorademo.cl", null, null, "Carla", true, false, null, "Consulta", null, null, "Active", null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, null, "Client", "consulta@importadorademo.cl" },
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000012"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "solicitud@importadorademo.cl", null, null, "Sergio", true, false, null, "Solicitante", null, null, "Pending", null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, null, "Client", "solicitud@importadorademo.cl" }
                });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000008"), new Guid("11111111-0007-0007-0007-000000000006"), "HLXU2023001", "40RF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-020301", "GateIn", null, null, 26800m },
                    { new Guid("22222222-0008-0008-0008-000000000010"), new Guid("11111111-0007-0007-0007-000000000008"), "HLXU2023003", "20DV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-020303", "Empty", null, null, 16900m }
                });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage" },
                values: new object[] { new Guid("11111111-0007-0007-0007-000000000007"), "HLCUVAP260300720", null, "HLCUBKG2603072", new Guid("c3d4e5f6-0003-0003-0003-000000000040"), "Shanghai Wine Trading Ltd", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 12, 2, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), 4100m, "USD", null, null, false, false, null, null, null, null, null, "Shanghai, China", "Shanghai (CNSHA)", "Valparaiso (CLVAP)", "Export", "Importadora Demo SpA", "Loaded", "Santos Express", null, "2611N" });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000009"), 95000m, new Guid("11111111-0007-0007-0007-000000000006"), "GateIn", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Gate In - 40RF (San Antonio)", true, null, null, "Pending", 18050m, 19m, 113050m },
                    { new Guid("33333333-0009-0009-0009-000000000010"), 45000m, new Guid("11111111-0007-0007-0007-000000000006"), "BL_FEE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "BL Documentation Fee (export)", true, null, null, "Pending", 8550m, 19m, 53550m },
                    { new Guid("33333333-0009-0009-0009-000000000012"), 820m, new Guid("11111111-0007-0007-0007-000000000008"), "GateIn", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Gate In - 20DV (Arica)", true, null, null, "Pending", 106.60m, 13m, 926.60m }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("5930f642-9936-0c5d-e985-2746431ae506"), new Guid("2477f174-66b3-8275-4ef4-b84902cdbb81"), new Guid("227e87c5-a8d9-51c6-e0e9-373dc6d2c60b") },
                    { new Guid("9aad4ae3-e43b-2fb4-92a5-bb181af7f9f4"), new Guid("208dfafe-5331-55c8-f20c-1fedbb16b3cb"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c") },
                    { new Guid("bb9d489a-77a2-b2d6-c0f6-2df2ff63a1ff"), new Guid("2477f174-66b3-8275-4ef4-b84902cdbb81"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c") },
                    { new Guid("f5904c22-d3b8-747e-062e-48c5d83ccc9d"), new Guid("d7f60fcb-88d4-54d3-54fb-d63def5c9613"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c") }
                });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CompletedAt", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "ModifiedAt", "ModifiedBy", "OrderNumber", "OrderType", "RequestedAt", "Status" },
                values: new object[] { new Guid("aaaaaaaa-0010-0010-0010-000000000004"), new Guid("11111111-0007-0007-0007-000000000006"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Recepción de contenedor reefer HLXU2023001 para exportación", null, null, "SO-2026-00004", "GateIn", new DateTime(2026, 10, 15, 9, 0, 0, 0, DateTimeKind.Utc), "Pending" });

            migrationBuilder.InsertData(
                table: "ShipmentAccessRules",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId" },
                values: new object[,]
                {
                    { new Guid("010d51d4-8d8a-cdee-e387-383102425314"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Carrier", new Guid("cc4c1544-f4f5-8f00-9bb1-8241a64e8d54") },
                    { new Guid("027d8ece-d35e-1127-2684-ab43766cda78"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0") },
                    { new Guid("03889b31-de43-5ee6-6c2c-b20949ba0b68"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0") },
                    { new Guid("040ee41b-f92d-b018-afb7-a8a6bb53fdfb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("4900513d-d3e1-f3e2-5b43-c0e000a389f0") },
                    { new Guid("041b6896-937b-270e-a33a-f7493f2cadaf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("1145e12a-7014-9221-40ff-2d3754ffd29f") },
                    { new Guid("0472449f-0179-c774-debc-2e4ccb57d8d1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("032cc54e-cbbe-5272-cab1-32a3bb291f82") },
                    { new Guid("06469c53-5eba-efc9-7230-1fd645f76ffb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("3eac8579-97de-40ff-10b5-ba572613f061") },
                    { new Guid("06da8896-2839-fabf-39bb-f8615fb01826"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("95d43dff-917c-dd4d-eeb5-c49731913c7c") },
                    { new Guid("0780de5d-f606-3521-8200-a7e75edb52d5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("c8157abe-83ae-64bb-ddbb-b4681e99c33f") },
                    { new Guid("07a5e3b1-ac08-3d1d-7da0-50a78f5527f9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("ca92d241-6346-1541-212a-e1403a95cb9f") },
                    { new Guid("0abc8a0c-ba8a-a05f-180a-6597ea4301fb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("557bba04-d0ce-91b0-c765-277b91222bf7") },
                    { new Guid("0afc2185-32f2-5fb2-74f4-5c12b1f165f2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("ac5533f1-b185-fe85-07a0-89a89adf382d") },
                    { new Guid("0c0b2d4b-c674-7c51-6f8d-e54acf0cb89f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Carrier", new Guid("0334bbd0-ffd8-1995-7db7-fdf3d95d6729") },
                    { new Guid("0d2c2750-0e0c-b7c7-5811-6bfa26c36d77"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("160ab007-ea1a-063f-52dd-e10cf1dcd00b") },
                    { new Guid("0dba0bf2-e494-61db-33b1-be24cd21c0ef"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("ca92d241-6346-1541-212a-e1403a95cb9f") },
                    { new Guid("0ee2c60a-5eca-e8b1-52dd-11e339055a41"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("6678139a-eb5f-24c3-d404-7ffcfbf1dde8") },
                    { new Guid("10777d2e-ff0f-dfe2-f5a8-cb1f7d92d4f3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("3ff1f234-1eda-6801-33a1-ce8538693d6e") },
                    { new Guid("14bd1a99-3b3d-6563-f136-c5be8e6da3b4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("93f658d0-ee65-42f2-a3da-646f54feb2d6") },
                    { new Guid("15322d9c-787e-cd71-4a5e-e61145250aa0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("7cc9c738-4673-e0c2-9a01-df7b48b28b7c") },
                    { new Guid("1656005a-c180-e36b-5fb6-b37ee190f74b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("bdcf5a98-4a99-386e-fe2f-1da80f99aa1e") },
                    { new Guid("17f3977e-2817-3583-b9f6-a21229905b19"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("ac5533f1-b185-fe85-07a0-89a89adf382d") },
                    { new Guid("1a906fed-f454-454e-6318-19b31e95c00d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("947b0092-be0a-4670-bf9f-46eda7d14ab0") },
                    { new Guid("1b4ecae0-51e4-b54b-4469-81f8e1ae8568"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("0334bbd0-ffd8-1995-7db7-fdf3d95d6729") },
                    { new Guid("1b67de2e-c9b3-2b49-b206-56b50045032d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("5fa67043-8f48-599c-fded-6ea2b5ca1488") },
                    { new Guid("1d631bb0-e662-507c-093e-3a1f944edaf2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("6678139a-eb5f-24c3-d404-7ffcfbf1dde8") },
                    { new Guid("1dbec5e1-7b9b-18ac-22a7-c282c6cdb245"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("7cc9c738-4673-e0c2-9a01-df7b48b28b7c") },
                    { new Guid("2071e182-5129-175f-6d04-319ba61f6086"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("65dfa9ad-25d2-4a79-f36c-410bf77ddddc") },
                    { new Guid("20a66bc3-53e5-8e79-a5cd-4ce5856c32f5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("032cc54e-cbbe-5272-cab1-32a3bb291f82") },
                    { new Guid("24e69e40-eeab-54a8-3d4e-e857a93e17b1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("95d43dff-917c-dd4d-eeb5-c49731913c7c") },
                    { new Guid("251753ba-5d1b-40ca-85a3-761ebb5997d5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "CustomsAgency", new Guid("e7eb07a5-bc20-230e-0e88-997310452f02") },
                    { new Guid("27b837d7-6166-08af-c723-3d57348607cd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0") },
                    { new Guid("294c84c4-4b81-1937-48ca-586e5d535d69"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0") },
                    { new Guid("29c910f8-86b9-0c9e-ea00-cbbb7d47cd89"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("81d22eff-dca4-0ac0-bf3a-4f3df458d908") },
                    { new Guid("2aad96e9-dc7c-0e06-d42c-696a4150964a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("4900513d-d3e1-f3e2-5b43-c0e000a389f0") },
                    { new Guid("2b149e4a-a05c-1c2b-2ba2-c8141ef73e9c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("032cc54e-cbbe-5272-cab1-32a3bb291f82") },
                    { new Guid("2c38402f-99c3-d95c-eb1b-c8a31a911979"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("6678139a-eb5f-24c3-d404-7ffcfbf1dde8") },
                    { new Guid("2e45c4f1-5b9c-a815-ea89-48ff72d0803e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "ThirdParty", new Guid("bdcf5a98-4a99-386e-fe2f-1da80f99aa1e") },
                    { new Guid("2e9526d1-5e70-86e0-2a71-6f259d29221a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("bdcf5a98-4a99-386e-fe2f-1da80f99aa1e") },
                    { new Guid("2f0163ce-15d4-e791-7cb9-5febfbf61145"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "CustomsAgency", new Guid("cfd10f40-3f50-8ebf-fed3-ec080f6c29b5") },
                    { new Guid("3072d047-9ee7-d892-5ff6-4adf7ecd0b54"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, "FreightForwarder", "ThirdParty", new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332") },
                    { new Guid("30928b02-6326-dc13-7a99-d91b7fdd5bc3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("72baee95-6d9c-f671-270d-67a5ddbb42e5") },
                    { new Guid("30d952cc-25ba-d6b4-d5b1-e5c88eaaf591"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("a6e130a7-a1ea-abd1-aa46-e6b5c5b63391") },
                    { new Guid("31180664-fea8-336a-07eb-b4cae5e5fc2f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("cc502303-816b-de21-d681-447330a0e8c9") },
                    { new Guid("32690a57-b1b1-c8a7-7f3f-e55b7511f6f9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("a155eb78-8542-4978-a8dd-bd2b28929738") },
                    { new Guid("32a1438b-8958-e589-80c6-17ef8fd1b20c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("160ab007-ea1a-063f-52dd-e10cf1dcd00b") },
                    { new Guid("34cb5bed-66c7-edad-f228-d31fe86bbb69"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("e7eb07a5-bc20-230e-0e88-997310452f02") },
                    { new Guid("34ffd440-fb10-56d5-6c98-eb3cb9ff12f3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("9119a3f4-394d-695c-5267-b7cd696f7ba6") },
                    { new Guid("36344a4a-6760-075e-efbd-ecb945d444dd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("741f2950-8b45-5e2d-8065-fe163db727a6") },
                    { new Guid("372eedc2-342b-19ff-0b5c-92f6e03f2c5e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("ac5533f1-b185-fe85-07a0-89a89adf382d") },
                    { new Guid("3971f911-1710-2a19-a015-e642b88e4f9f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("cc502303-816b-de21-d681-447330a0e8c9") },
                    { new Guid("39ca777e-10d1-2692-40d0-20abdb4c1d1f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "ThirdParty", new Guid("f047ab5d-f553-96e9-9003-6fe15a0a5114") },
                    { new Guid("3a26cc23-eea8-5033-d004-1d0d9a65b7d0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("741f2950-8b45-5e2d-8065-fe163db727a6") },
                    { new Guid("3aa44a80-9f63-6f75-4802-df51e75ac991"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332") },
                    { new Guid("3b3f547b-ae4b-f532-d938-15b23330aa7a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("cbecbaf4-b71c-43d6-aae2-c344d6bf19ad") },
                    { new Guid("3ba841d2-ab0f-ed1b-0312-336875569e29"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "CustomsAgency", new Guid("c8157abe-83ae-64bb-ddbb-b4681e99c33f") },
                    { new Guid("3c4a641a-5e8b-3c23-3400-3738934dba37"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "ThirdParty", new Guid("1c3dd706-407f-2945-5b68-bea5dfd584e9") },
                    { new Guid("3e6f1f78-d711-7f8f-9fcb-7f22e222a15c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("bd6cab83-dabc-1619-6d2c-e324cd7d01a1") },
                    { new Guid("4042f116-b438-28d4-0b36-efde4ca07061"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("cc4c1544-f4f5-8f00-9bb1-8241a64e8d54") },
                    { new Guid("40e1d283-26fc-e646-05f6-04daaa579f00"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("122ef33e-f8f5-fb84-25cb-5176d10c40dc") },
                    { new Guid("421a7754-907f-1236-c71e-4c236102c701"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("72baee95-6d9c-f671-270d-67a5ddbb42e5") },
                    { new Guid("42797c44-5ef1-636d-2b04-4b4126fb59bd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("81d22eff-dca4-0ac0-bf3a-4f3df458d908") },
                    { new Guid("4298ebf0-e400-92b8-4012-d5189a19c86c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("c8157abe-83ae-64bb-ddbb-b4681e99c33f") },
                    { new Guid("429c0335-4e9c-1cbd-4413-027708584384"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("a155eb78-8542-4978-a8dd-bd2b28929738") },
                    { new Guid("42a6b84b-21c5-5337-0557-d60952e56ddf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("c8157abe-83ae-64bb-ddbb-b4681e99c33f") },
                    { new Guid("42e3b162-8199-d447-35cf-04f3ab6d1bb9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("2ac5eecb-38d8-6ec0-6d82-f03f854e976c") },
                    { new Guid("43205b4a-bc44-340e-779c-229bdfbad023"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Carrier", new Guid("3ff1f234-1eda-6801-33a1-ce8538693d6e") },
                    { new Guid("4371663b-2c32-eb51-3231-cab8986972af"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("cfd10f40-3f50-8ebf-fed3-ec080f6c29b5") },
                    { new Guid("43a61aeb-5499-b695-6d2e-d353a26e2807"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("4900513d-d3e1-f3e2-5b43-c0e000a389f0") },
                    { new Guid("4524e51e-00c0-978b-f8a4-1d0cc5f7fc56"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("032cc54e-cbbe-5272-cab1-32a3bb291f82") },
                    { new Guid("4552373e-8b24-a588-560e-17eda0460137"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("a6e130a7-a1ea-abd1-aa46-e6b5c5b63391") },
                    { new Guid("45987d46-1370-e066-5b82-5d4052a0fab1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("4900513d-d3e1-f3e2-5b43-c0e000a389f0") },
                    { new Guid("45c781d9-24f8-91c6-ac62-eaacff01f315"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("72baee95-6d9c-f671-270d-67a5ddbb42e5") },
                    { new Guid("4649623b-1041-4904-b0de-1ef347361ced"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332") },
                    { new Guid("46adbe3c-20ab-260a-f4c1-31a321e8591e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("1145e12a-7014-9221-40ff-2d3754ffd29f") },
                    { new Guid("47e25292-4619-bac0-0ece-379958f1524f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Shipper", new Guid("07343dd5-fa12-c8e8-a80f-26f110926989") },
                    { new Guid("48a49c20-2f55-70e9-5a32-30420ff4f191"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("cbecbaf4-b71c-43d6-aae2-c344d6bf19ad") },
                    { new Guid("48cdf5fe-df57-6259-5624-c70f64d51d56"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("5810f4b9-0e60-a000-5697-53177df94bb9") },
                    { new Guid("49cbf101-bc3d-5e3f-da09-58c328286f0e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("1145e12a-7014-9221-40ff-2d3754ffd29f") },
                    { new Guid("4a2db5f4-b95a-dc7f-1af9-d527c01e4cec"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("3ff1f234-1eda-6801-33a1-ce8538693d6e") },
                    { new Guid("4cb06fb4-757a-ec71-7e15-a181930a458a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("e112508b-beea-c895-9061-da03cb427883") },
                    { new Guid("4d076896-7c12-8639-7317-f190b7829b4e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "CustomsAgency", new Guid("fe2d4e42-d3b0-aac5-9026-5f918c61aca6") },
                    { new Guid("4d50588c-b82a-0108-ffe9-91f76b427b52"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("1c3dd706-407f-2945-5b68-bea5dfd584e9") },
                    { new Guid("4da9ce71-a545-4bd4-3ca3-aec28a330570"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("e112508b-beea-c895-9061-da03cb427883") },
                    { new Guid("4f708bb5-54b4-88cc-060d-1df522408281"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("a155eb78-8542-4978-a8dd-bd2b28929738") },
                    { new Guid("505eebc1-bd10-900f-0554-03d2cb6f4ce3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("f047ab5d-f553-96e9-9003-6fe15a0a5114") },
                    { new Guid("5156db2e-2806-e2e4-b3d5-6f28bd6926c2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332") },
                    { new Guid("5224b44f-6397-ffb6-4824-172308024f03"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("160ab007-ea1a-063f-52dd-e10cf1dcd00b") },
                    { new Guid("538ffd2c-4040-d01f-546d-6e6701b29d8f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("19ef0081-eb79-5a7c-e09c-62482731cd52") },
                    { new Guid("5479e129-31cc-3199-a790-25ab399cf5d3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("95d43dff-917c-dd4d-eeb5-c49731913c7c") },
                    { new Guid("56477c5b-146d-5936-ab86-ce2947c28400"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("f047ab5d-f553-96e9-9003-6fe15a0a5114") },
                    { new Guid("56e99889-a543-6cd3-6923-a1d96b527705"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "ThirdParty", new Guid("5810f4b9-0e60-a000-5697-53177df94bb9") },
                    { new Guid("57d921ca-471a-7daa-4dc8-cdc7326f9916"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("6678139a-eb5f-24c3-d404-7ffcfbf1dde8") },
                    { new Guid("58e014aa-d2e9-f2da-7606-f775b608af9a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("bdcf5a98-4a99-386e-fe2f-1da80f99aa1e") },
                    { new Guid("590275eb-74e7-f6b0-6b77-16a2ea4df1be"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332") },
                    { new Guid("5af0a6a1-44e1-c571-a3d4-e713a4c28c47"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Consignee", new Guid("f047ab5d-f553-96e9-9003-6fe15a0a5114") },
                    { new Guid("5c43e351-0ef6-ed31-eede-a70f0d95e955"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("72c736f3-0830-08da-2ba1-aef0b2ec3e81") },
                    { new Guid("5d7a906f-0457-debf-16cc-8cc7d3591cc9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "ThirdParty", new Guid("cc502303-816b-de21-d681-447330a0e8c9") },
                    { new Guid("5ddcc49d-e7e4-4361-8e23-784af9d09b59"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("9119a3f4-394d-695c-5267-b7cd696f7ba6") },
                    { new Guid("5e581018-b0aa-f5af-a276-04bce7c91ee2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("cbecbaf4-b71c-43d6-aae2-c344d6bf19ad") },
                    { new Guid("5f020f3a-d3a7-5134-b547-331572363e8e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("93f658d0-ee65-42f2-a3da-646f54feb2d6") },
                    { new Guid("5f5e7f62-a9ef-f788-7d07-afabbadb0fad"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("07343dd5-fa12-c8e8-a80f-26f110926989") },
                    { new Guid("6046e3eb-e5f8-fe89-4dab-66435ec03f34"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("a6e130a7-a1ea-abd1-aa46-e6b5c5b63391") },
                    { new Guid("614d25ef-5223-81a4-348b-bcfdf2897acd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("7cc9c738-4673-e0c2-9a01-df7b48b28b7c") },
                    { new Guid("621d5ee2-ec4d-c1db-49cf-a49b3d72693d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("cfd10f40-3f50-8ebf-fed3-ec080f6c29b5") },
                    { new Guid("63ecfc33-601b-ad09-9d78-112aa1ef13f4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("122ef33e-f8f5-fb84-25cb-5176d10c40dc") },
                    { new Guid("647598ba-60d9-01d8-760c-24592ffc6c27"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("7cc9c738-4673-e0c2-9a01-df7b48b28b7c") },
                    { new Guid("64a91391-ce1e-501f-4bda-2ebba3452cc3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("9119a3f4-394d-695c-5267-b7cd696f7ba6") },
                    { new Guid("65daff7d-bee6-9eb0-6b14-73e6600829ad"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("1c3dd706-407f-2945-5b68-bea5dfd584e9") },
                    { new Guid("6609d9e9-c604-2163-6458-89bd9bb93bd1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("e112508b-beea-c895-9061-da03cb427883") },
                    { new Guid("66c7c7ed-0fb1-b53a-9a65-ab29dd47e2e8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("2ac5eecb-38d8-6ec0-6d82-f03f854e976c") },
                    { new Guid("6766088f-1bb3-bcbb-b05b-c8f71c1c2924"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("3eac8579-97de-40ff-10b5-ba572613f061") },
                    { new Guid("67c9ec3a-5f77-cd99-4a80-89420ec9e32e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("cbecbaf4-b71c-43d6-aae2-c344d6bf19ad") },
                    { new Guid("69d37a7e-cc1f-500e-5768-e57b6fabb0dc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("ca92d241-6346-1541-212a-e1403a95cb9f") },
                    { new Guid("6baa464d-0fde-c9b3-c91c-db76ee89e118"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("a6e130a7-a1ea-abd1-aa46-e6b5c5b63391") },
                    { new Guid("6c64e880-71dc-9acc-f65c-b83b10e15a29"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("947b0092-be0a-4670-bf9f-46eda7d14ab0") },
                    { new Guid("6c882143-7f2c-6b4c-22a3-1efd49850382"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("cc4c1544-f4f5-8f00-9bb1-8241a64e8d54") },
                    { new Guid("6d94fa6b-5851-b680-e57e-b76376d3712c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("557bba04-d0ce-91b0-c765-277b91222bf7") },
                    { new Guid("6e60cc41-5c2d-5dd6-331b-2b9ddc3680ad"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("ac5533f1-b185-fe85-07a0-89a89adf382d") },
                    { new Guid("6e918218-fcff-0616-7960-883141a8d6ca"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("ca92d241-6346-1541-212a-e1403a95cb9f") },
                    { new Guid("6ed3bea3-c94c-fbd1-ac02-f6b389e5fe51"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "ThirdParty", new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332") },
                    { new Guid("71697283-91d4-1c52-b6db-3348c4690813"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("2ac5eecb-38d8-6ec0-6d82-f03f854e976c") },
                    { new Guid("726b16f2-fa0d-1968-8e15-3023810576bb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("bd6cab83-dabc-1619-6d2c-e324cd7d01a1") },
                    { new Guid("74268720-8e17-8c02-727a-9ab210344516"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("741f2950-8b45-5e2d-8065-fe163db727a6") },
                    { new Guid("74f6f58b-5ef0-2510-a464-821e48c21042"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("bd6cab83-dabc-1619-6d2c-e324cd7d01a1") },
                    { new Guid("757e48dd-3e30-eb14-d402-d169df515eb8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("72c736f3-0830-08da-2ba1-aef0b2ec3e81") },
                    { new Guid("7795c572-e951-5f14-f881-cf2f07872b82"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("95d43dff-917c-dd4d-eeb5-c49731913c7c") },
                    { new Guid("78ac3e40-5908-b351-4a8e-bd6a9ebd40d0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("a6e130a7-a1ea-abd1-aa46-e6b5c5b63391") },
                    { new Guid("79a0267e-3384-8876-7473-ab1f55fdbc83"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("cfd10f40-3f50-8ebf-fed3-ec080f6c29b5") },
                    { new Guid("7c1d56bf-3a00-c127-68a5-6f3a6fc2727a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("72baee95-6d9c-f671-270d-67a5ddbb42e5") },
                    { new Guid("7c3379d3-9575-df91-4a5d-78621bd7c7ea"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("4900513d-d3e1-f3e2-5b43-c0e000a389f0") },
                    { new Guid("7daf3c02-9061-4ff6-2877-a1fa44fa4a65"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("160ab007-ea1a-063f-52dd-e10cf1dcd00b") },
                    { new Guid("7e1c042b-476a-3c9e-1e7b-2c5df3c89348"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("5fa67043-8f48-599c-fded-6ea2b5ca1488") },
                    { new Guid("7f6bb3c7-c81f-b439-e804-1caeefa025dd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("5fa67043-8f48-599c-fded-6ea2b5ca1488") },
                    { new Guid("7fe2dc65-4508-baf6-14cf-051be7083497"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("947b0092-be0a-4670-bf9f-46eda7d14ab0") },
                    { new Guid("821d828e-6133-9e4e-a921-94b4c526ca27"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Carrier", new Guid("cfd10f40-3f50-8ebf-fed3-ec080f6c29b5") },
                    { new Guid("8242b4a9-657d-a34b-ab6f-2a07dbf4895c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("122ef33e-f8f5-fb84-25cb-5176d10c40dc") },
                    { new Guid("8362f856-f5cd-289b-00cd-d45c84a84780"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Consignee", new Guid("5810f4b9-0e60-a000-5697-53177df94bb9") },
                    { new Guid("847ca6d5-fef3-d970-a9b4-6558953783f9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("fe2d4e42-d3b0-aac5-9026-5f918c61aca6") },
                    { new Guid("87b37bd6-2edc-d8f0-0e61-4f8f22d8f6d9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Carrier", new Guid("c8157abe-83ae-64bb-ddbb-b4681e99c33f") },
                    { new Guid("8825579f-fc7d-412c-bc32-74034b16b5de"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("741f2950-8b45-5e2d-8065-fe163db727a6") },
                    { new Guid("88805483-ef38-f504-0b87-9490722d88fe"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("0334bbd0-ffd8-1995-7db7-fdf3d95d6729") },
                    { new Guid("896bb155-f937-acc3-8766-97e295937dad"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("ca92d241-6346-1541-212a-e1403a95cb9f") },
                    { new Guid("897dadab-8beb-2996-f9e0-4992031c4e12"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("72baee95-6d9c-f671-270d-67a5ddbb42e5") },
                    { new Guid("8c4543b4-45e6-28c6-407e-d8203b677242"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Consignee", new Guid("cc502303-816b-de21-d681-447330a0e8c9") },
                    { new Guid("8c511f1d-9a17-aa9e-5b7b-6917943ca3ac"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("a6e130a7-a1ea-abd1-aa46-e6b5c5b63391") },
                    { new Guid("8c7a9390-a08d-3d82-7473-01ea6526ccf9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("9119a3f4-394d-695c-5267-b7cd696f7ba6") },
                    { new Guid("8cfbd24a-af9a-c215-4cd1-4aaa0cde2533"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("5fa67043-8f48-599c-fded-6ea2b5ca1488") },
                    { new Guid("8ded4ee9-81c3-f5d0-8855-6dfd251ada29"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Consignee", new Guid("19ef0081-eb79-5a7c-e09c-62482731cd52") },
                    { new Guid("8ee57936-9694-c99d-5e86-fb2a9e34fb05"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("947b0092-be0a-4670-bf9f-46eda7d14ab0") },
                    { new Guid("8fac5dd6-bc91-bd85-02fb-d13cca171026"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("5810f4b9-0e60-a000-5697-53177df94bb9") },
                    { new Guid("909211e6-5af5-5a5d-f1fd-a6adcc2c9202"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("81d22eff-dca4-0ac0-bf3a-4f3df458d908") },
                    { new Guid("9287fdb1-6268-b086-80bd-94119a7681fa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("7cc9c738-4673-e0c2-9a01-df7b48b28b7c") },
                    { new Guid("94089062-2d6a-1d10-48be-5750cbb89efc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Consignee", new Guid("bdcf5a98-4a99-386e-fe2f-1da80f99aa1e") },
                    { new Guid("940c605b-65d7-70e9-d0ec-154024f84808"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("1c3dd706-407f-2945-5b68-bea5dfd584e9") },
                    { new Guid("94b2afd5-6b57-26e9-c1d8-7fa668b08cf1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("122ef33e-f8f5-fb84-25cb-5176d10c40dc") },
                    { new Guid("94efb966-f5b2-29c8-4382-ded15360f3e9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("07343dd5-fa12-c8e8-a80f-26f110926989") },
                    { new Guid("95060891-2e57-1691-1406-a6ae53955e4f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("f047ab5d-f553-96e9-9003-6fe15a0a5114") },
                    { new Guid("9574b879-7550-caf5-4571-430001fe7ccf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("e112508b-beea-c895-9061-da03cb427883") },
                    { new Guid("957b2f32-5149-4b3f-d877-d9d63c8f84a1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("72c736f3-0830-08da-2ba1-aef0b2ec3e81") },
                    { new Guid("973c4872-c7ac-84f5-23b8-c359071d62d9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("160ab007-ea1a-063f-52dd-e10cf1dcd00b") },
                    { new Guid("98bcc1b9-d949-c3f3-646e-368c32f3ded3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("cc4c1544-f4f5-8f00-9bb1-8241a64e8d54") },
                    { new Guid("99a492a0-de45-e49c-63c5-43089474bc33"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("19ef0081-eb79-5a7c-e09c-62482731cd52") },
                    { new Guid("9a221b20-6552-1f28-c587-a9d3cbd156d0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("e7eb07a5-bc20-230e-0e88-997310452f02") },
                    { new Guid("9a4d5735-85ec-1b50-ce95-2d7c2d42ed82"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("81d22eff-dca4-0ac0-bf3a-4f3df458d908") },
                    { new Guid("9c642deb-1722-9fc9-4051-67860f8f3a94"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("f047ab5d-f553-96e9-9003-6fe15a0a5114") },
                    { new Guid("9e615364-af15-4484-4ad7-029b05c0c717"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("07343dd5-fa12-c8e8-a80f-26f110926989") },
                    { new Guid("9e77dd6e-ec7d-357e-9ec9-0f5872f40b6b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("1145e12a-7014-9221-40ff-2d3754ffd29f") },
                    { new Guid("a0251783-0ed3-accb-407c-4bcc84d98acb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("93f658d0-ee65-42f2-a3da-646f54feb2d6") },
                    { new Guid("a2b4577d-6fd2-5bab-e6e2-5f2e5936462c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("a155eb78-8542-4978-a8dd-bd2b28929738") },
                    { new Guid("a3291c2f-76c6-efe4-0faf-b2008c4c43be"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("9119a3f4-394d-695c-5267-b7cd696f7ba6") },
                    { new Guid("a603b74d-ed71-4582-028d-292d126b5ce8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("557bba04-d0ce-91b0-c765-277b91222bf7") },
                    { new Guid("a952cb8b-e76f-478b-3944-8a20210a0021"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("72c736f3-0830-08da-2ba1-aef0b2ec3e81") },
                    { new Guid("a96cf84e-2d74-c8d7-08a1-fac00d3a3e50"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("3ff1f234-1eda-6801-33a1-ce8538693d6e") },
                    { new Guid("a9f23aed-68a5-e6bd-966d-5009cdfc5d38"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "CustomsAgency", new Guid("3ff1f234-1eda-6801-33a1-ce8538693d6e") },
                    { new Guid("aaf55402-dcf0-c382-bad0-7864e2e45c76"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("ca92d241-6346-1541-212a-e1403a95cb9f") },
                    { new Guid("ab19fe50-c320-938b-3771-ad86898eb672"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("c8157abe-83ae-64bb-ddbb-b4681e99c33f") },
                    { new Guid("abc072ab-4bc9-1c55-6482-33d34f31a9f1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("3eac8579-97de-40ff-10b5-ba572613f061") },
                    { new Guid("af249dea-7de3-74a2-1433-014fce1cce5b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("65dfa9ad-25d2-4a79-f36c-410bf77ddddc") },
                    { new Guid("aff9c28f-9185-18a7-7b8f-41959210f1ea"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("95d43dff-917c-dd4d-eeb5-c49731913c7c") },
                    { new Guid("b0ca6dd6-a58e-35a3-5459-de9a19117992"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("7cc9c738-4673-e0c2-9a01-df7b48b28b7c") },
                    { new Guid("b117fe73-e202-0085-2977-0f10b6f8dc6b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("032cc54e-cbbe-5272-cab1-32a3bb291f82") },
                    { new Guid("b130d10f-b64f-2e58-8365-81cc9fc40800"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("0334bbd0-ffd8-1995-7db7-fdf3d95d6729") },
                    { new Guid("b1e422f0-7170-90a4-78ee-0b68f99be754"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("93f658d0-ee65-42f2-a3da-646f54feb2d6") },
                    { new Guid("b24dec4a-cd0c-9106-0c12-f4bb68bd3cbb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("1145e12a-7014-9221-40ff-2d3754ffd29f") },
                    { new Guid("b2696b0d-e253-4de4-7fff-8fccc68320b2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Consignee", new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332") },
                    { new Guid("b30a8f44-62dc-824b-0a29-5abe231960cf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("3eac8579-97de-40ff-10b5-ba572613f061") },
                    { new Guid("b456458b-9b60-bbd6-f93d-b46d00e54530"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("cbecbaf4-b71c-43d6-aae2-c344d6bf19ad") },
                    { new Guid("b470fe19-5297-c9cb-b037-39178256df53"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "ThirdParty", new Guid("65dfa9ad-25d2-4a79-f36c-410bf77ddddc") },
                    { new Guid("b496394a-7d2b-5faf-6700-edf0e478bc50"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("2ac5eecb-38d8-6ec0-6d82-f03f854e976c") },
                    { new Guid("b55d88d9-efe7-43fd-bc11-279be867f93c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("5810f4b9-0e60-a000-5697-53177df94bb9") },
                    { new Guid("b59380f8-1560-c3ad-9f59-8bb1c46b0f35"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("19ef0081-eb79-5a7c-e09c-62482731cd52") },
                    { new Guid("b697cbb0-6ce6-5558-9f99-f60d9c2bc6ec"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("e112508b-beea-c895-9061-da03cb427883") },
                    { new Guid("b6a9e689-e0ca-2201-ec70-f04f9d2385ae"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("947b0092-be0a-4670-bf9f-46eda7d14ab0") },
                    { new Guid("b6ebbb91-c28c-f156-2859-b065dccd4dca"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("6678139a-eb5f-24c3-d404-7ffcfbf1dde8") },
                    { new Guid("b78b7f5a-800a-db8f-3f30-aa7ef4dbb34b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("557bba04-d0ce-91b0-c765-277b91222bf7") },
                    { new Guid("b7b14efa-dcea-e412-85d4-8b72fbadc2cc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("557bba04-d0ce-91b0-c765-277b91222bf7") },
                    { new Guid("ba142a8c-e51a-92ff-25a2-3baa3e567086"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("93f658d0-ee65-42f2-a3da-646f54feb2d6") },
                    { new Guid("bad28780-396c-e7e1-8307-031ef5265f06"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("a155eb78-8542-4978-a8dd-bd2b28929738") },
                    { new Guid("baf7e271-30b8-8d8a-b3d0-0b2b1d212e7d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("741f2950-8b45-5e2d-8065-fe163db727a6") },
                    { new Guid("bc3d006a-fdc8-f595-0209-2dd959b3366a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("9119a3f4-394d-695c-5267-b7cd696f7ba6") },
                    { new Guid("bd154f5f-8d67-519e-570a-8112d5385e39"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("81d22eff-dca4-0ac0-bf3a-4f3df458d908") },
                    { new Guid("bd4d4477-d42d-01a8-35ef-48330dc161ba"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("65dfa9ad-25d2-4a79-f36c-410bf77ddddc") },
                    { new Guid("bd92ab53-8c00-df93-d90d-fc31bd1c44c7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("5810f4b9-0e60-a000-5697-53177df94bb9") },
                    { new Guid("bf0f554a-88fd-b0af-9528-f20d9652bde6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("65dfa9ad-25d2-4a79-f36c-410bf77ddddc") },
                    { new Guid("bfbbb097-ad4d-b483-42ca-3cd4f4b4f33e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("122ef33e-f8f5-fb84-25cb-5176d10c40dc") },
                    { new Guid("c0cce1f2-bc75-ba23-f821-df85fa227bef"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("a155eb78-8542-4978-a8dd-bd2b28929738") },
                    { new Guid("c120a136-1c7c-16c7-3ffc-1b4d687a9ad8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Shipper", new Guid("3ff1f234-1eda-6801-33a1-ce8538693d6e") },
                    { new Guid("c141b448-7b07-b9ff-1419-f42969002eef"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("81d22eff-dca4-0ac0-bf3a-4f3df458d908") },
                    { new Guid("c27e7f95-c695-de79-1a6c-77f83520649c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("ac5533f1-b185-fe85-07a0-89a89adf382d") },
                    { new Guid("c50d5427-463b-3112-578d-f4443af0de0f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("3eac8579-97de-40ff-10b5-ba572613f061") },
                    { new Guid("c55e11c1-445b-2ceb-fa9c-42fa7d32a375"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("bd6cab83-dabc-1619-6d2c-e324cd7d01a1") },
                    { new Guid("c638329f-82cc-c199-4452-fb571b1e365f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0") },
                    { new Guid("c63962d0-bebe-bba3-bd58-72e8ff1ff20c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0") },
                    { new Guid("caf8c270-a9dd-ea27-05cf-946f86c3afa2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("122ef33e-f8f5-fb84-25cb-5176d10c40dc") },
                    { new Guid("cb7965bb-3d07-6a52-ec63-2e6e798011a8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("bd6cab83-dabc-1619-6d2c-e324cd7d01a1") },
                    { new Guid("ce4c2405-a127-ce82-7591-29e60d63c47e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("65dfa9ad-25d2-4a79-f36c-410bf77ddddc") },
                    { new Guid("cfd48abf-7e89-405a-fa17-6ec0c7b9857a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("fe2d4e42-d3b0-aac5-9026-5f918c61aca6") },
                    { new Guid("d1985ab7-5eab-a949-b01a-5b6057cf2274"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("1c3dd706-407f-2945-5b68-bea5dfd584e9") },
                    { new Guid("d1c40053-b297-3747-07fa-86cfa2eb4e46"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("93f658d0-ee65-42f2-a3da-646f54feb2d6") },
                    { new Guid("d39f04a9-f04e-a4c7-e1fc-bacee5a3d6d2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Carrier", new Guid("e7eb07a5-bc20-230e-0e88-997310452f02") },
                    { new Guid("d44353bb-5eac-b868-7861-6f5878860718"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("5fa67043-8f48-599c-fded-6ea2b5ca1488") },
                    { new Guid("d46df13e-3333-6765-0a90-e3793343be54"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("160ab007-ea1a-063f-52dd-e10cf1dcd00b") },
                    { new Guid("d4c971c8-dc75-0e08-8eb3-6140558f93e4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("bdcf5a98-4a99-386e-fe2f-1da80f99aa1e") },
                    { new Guid("d58ce3f5-9e33-fe8a-4981-65037d44af2d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("72c736f3-0830-08da-2ba1-aef0b2ec3e81") },
                    { new Guid("d5fb8f14-501b-77c6-6532-dd5c707bcfb6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("1145e12a-7014-9221-40ff-2d3754ffd29f") },
                    { new Guid("d63cebae-75bb-a0a1-e8e2-a091564a59b9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("72c736f3-0830-08da-2ba1-aef0b2ec3e81") },
                    { new Guid("d6d69f1f-2b54-1101-8bc1-aa035746dac0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Carrier", new Guid("95d43dff-917c-dd4d-eeb5-c49731913c7c") },
                    { new Guid("d9c3071a-b3cc-d307-9ae7-bf179e989491"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("e112508b-beea-c895-9061-da03cb427883") },
                    { new Guid("da427540-6fc5-453a-337a-f95f7789891e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("557bba04-d0ce-91b0-c765-277b91222bf7") },
                    { new Guid("daa5a48e-7faf-f440-00ed-d6f290ef53d6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Customer", new Guid("0334bbd0-ffd8-1995-7db7-fdf3d95d6729") },
                    { new Guid("db2d9016-197b-30b9-34ed-f61a6aa010c6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("741f2950-8b45-5e2d-8065-fe163db727a6") },
                    { new Guid("dcc8ba2c-2edb-5dd3-d347-3febfaae31dd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("e7eb07a5-bc20-230e-0e88-997310452f02") },
                    { new Guid("de251b6a-681e-f468-c759-ba9e3a135a0c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("3eac8579-97de-40ff-10b5-ba572613f061") },
                    { new Guid("dfa110eb-b551-ad55-b4bb-b66a15c62f0a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("e7eb07a5-bc20-230e-0e88-997310452f02") },
                    { new Guid("e29f23be-4fe7-e2d5-fab5-17f0d6ee3221"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("2ac5eecb-38d8-6ec0-6d82-f03f854e976c") },
                    { new Guid("e2c15bd3-cd74-a740-c820-d98d7f2c1755"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "ThirdParty", new Guid("19ef0081-eb79-5a7c-e09c-62482731cd52") },
                    { new Guid("e4eff5c6-31ee-5c3e-5a4a-31abe13b6b86"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("bd6cab83-dabc-1619-6d2c-e324cd7d01a1") },
                    { new Guid("e52c2c06-c8d4-1643-0c5a-142353c4ae09"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "CustomsAgency", new Guid("0334bbd0-ffd8-1995-7db7-fdf3d95d6729") },
                    { new Guid("e5fc0f1b-12f4-d1f7-737c-9a3781cb5b2a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("5fa67043-8f48-599c-fded-6ea2b5ca1488") },
                    { new Guid("e6a7d610-7db4-1053-729a-a996c02bbae7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "CustomsAgency", new Guid("07343dd5-fa12-c8e8-a80f-26f110926989") },
                    { new Guid("e9d56994-87d7-dc46-902e-f0ba14c43496"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("947b0092-be0a-4670-bf9f-46eda7d14ab0") },
                    { new Guid("ead4bb59-96b2-a7f7-0cb0-e1b86d186e0b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("cc4c1544-f4f5-8f00-9bb1-8241a64e8d54") },
                    { new Guid("ec7b64b1-483f-47eb-f7eb-de0b24bdc782"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("4900513d-d3e1-f3e2-5b43-c0e000a389f0") },
                    { new Guid("eca7e100-5a9b-2cc8-59b7-e23ac0083c56"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Consignee", new Guid("fe2d4e42-d3b0-aac5-9026-5f918c61aca6") },
                    { new Guid("ed906fdc-af9a-3f8b-978c-86c602fbccf9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("cc502303-816b-de21-d681-447330a0e8c9") },
                    { new Guid("edb8d86b-9a08-b675-7f36-4325d664c9f9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Shipper", new Guid("cfd10f40-3f50-8ebf-fed3-ec080f6c29b5") },
                    { new Guid("ef578090-e366-8eed-a9a6-648604da07b0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, "FreightForwarder", "Consignee", new Guid("cc502303-816b-de21-d681-447330a0e8c9") },
                    { new Guid("f08e09ec-8049-1256-1870-600628cf1a60"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Carrier", new Guid("07343dd5-fa12-c8e8-a80f-26f110926989") },
                    { new Guid("f1109e2e-2cd5-3c56-7db7-22941a2771d6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "CustomsAgency", new Guid("19ef0081-eb79-5a7c-e09c-62482731cd52") },
                    { new Guid("f40bcf95-a335-b13c-1a3a-6bc1870df8b9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "Carrier", new Guid("fe2d4e42-d3b0-aac5-9026-5f918c61aca6") },
                    { new Guid("f5cb4db0-d3a1-3725-3473-fe15b924dba3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("032cc54e-cbbe-5272-cab1-32a3bb291f82") },
                    { new Guid("f6832259-179f-a8fb-8ffe-b6abc4c3a44e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Customer", new Guid("fe2d4e42-d3b0-aac5-9026-5f918c61aca6") },
                    { new Guid("f69dcfc4-e4a6-f436-b169-b9322daad293"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("1c3dd706-407f-2945-5b68-bea5dfd584e9") },
                    { new Guid("f700f67e-c7c1-2951-e930-b482ff2b6eed"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Denied", null, null, null, "Carrier", new Guid("cc502303-816b-de21-d681-447330a0e8c9") },
                    { new Guid("f9c715ab-5fe7-8cc0-cd61-f45973771151"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "OnGrant", null, null, null, "ThirdParty", new Guid("cc4c1544-f4f5-8f00-9bb1-8241a64e8d54") },
                    { new Guid("fa22d583-b3e9-dc80-c89c-7637781279c5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "Shipper", new Guid("72baee95-6d9c-f671-270d-67a5ddbb42e5") },
                    { new Guid("fb8f7c92-ff67-bb8b-de6a-41ebfe461a18"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "ThirdParty", new Guid("ac5533f1-b185-fe85-07a0-89a89adf382d") },
                    { new Guid("fc9814e8-e8cc-95ce-a023-248a48704c2c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("2ac5eecb-38d8-6ec0-6d82-f03f854e976c") },
                    { new Guid("fe48f4fa-7627-4176-7134-0cc98899776d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("6678139a-eb5f-24c3-d404-7ffcfbf1dde8") },
                    { new Guid("febe8c1e-5992-1534-a39c-ed590f98418c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, null, "CustomsAgency", new Guid("cbecbaf4-b71c-43d6-aae2-c344d6bf19ad") }
                });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[,]
                {
                    { new Guid("920f9473-1eaa-e2fa-664c-630fda43dcb3"), new Guid("11111111-0007-0007-0007-000000000006"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Shipper", "Seed" },
                    { new Guid("b65a3c60-f2d7-5b2c-22d2-c24a2594c519"), new Guid("11111111-0007-0007-0007-000000000008"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Shipper", "Seed" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "RoleId", "RoleName", "UserId" },
                values: new object[,]
                {
                    { new Guid("1c70376e-431c-3994-dd5c-464e0f1405f9"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new Guid("d4e5f6a7-0004-0004-0004-000000000030") },
                    { new Guid("3c47e0d2-5bc6-59f8-dc0c-12f7eb10e017"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new Guid("d4e5f6a7-0004-0004-0004-000000000010") },
                    { new Guid("3f8e776c-1c97-8168-2886-4ab337a4749e"), new Guid("a084c0fb-db38-005b-3d5f-926ed28ed6df"), "OrgViewer", new Guid("d4e5f6a7-0004-0004-0004-000000000011") },
                    { new Guid("a87fd524-45a0-5b06-c5bc-b848addc0849"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new Guid("d4e5f6a7-0004-0004-0004-000000000020") }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username" },
                values: new object[] { new Guid("d4e5f6a7-0004-0004-0004-000000000050"), new Guid("c3d4e5f6-0003-0003-0003-000000000050"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "admin@logisticaandina.cl", null, null, "Andrea", true, false, null, "Andina", null, null, "Active", null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, null, "Client", "admin@logisticaandina.cl" });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[] { new Guid("22222222-0008-0008-0008-000000000009"), new Guid("11111111-0007-0007-0007-000000000007"), "HLXU2023002", "20DV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-020302", "OnBoard", null, null, 17400m });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[] { new Guid("33333333-0009-0009-0009-000000000011"), 150000m, new Guid("11111111-0007-0007-0007-000000000007"), "THC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 20DV (export)", true, null, null, "Pending", 28500m, 19m, 178500m });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[] { new Guid("81adf541-7dc4-a75b-098f-d639ee7f15ab"), new Guid("11111111-0007-0007-0007-000000000007"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Shipper", "Seed" });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "RoleId", "RoleName", "UserId" },
                values: new object[] { new Guid("e2805dbb-3327-1b7d-aecd-7165a75c5d82"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new Guid("d4e5f6a7-0004-0004-0004-000000000050") });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClientId_MembershipStatus",
                table: "Users",
                columns: new[] { "ClientId", "MembershipStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_MatchCode",
                table: "Clients",
                column: "MatchCode",
                unique: true,
                filter: "\"MatchCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_RegistrationStatus",
                table: "Clients",
                column: "RegistrationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BillsOfLading_BookingNumber",
                table: "BillsOfLading",
                column: "BookingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationDocuments_ClientId",
                table: "OrganizationDocuments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAccessRules_ShipmentActionId_Role_OrganizationType",
                table: "ShipmentAccessRules",
                columns: new[] { "ShipmentActionId", "Role", "OrganizationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentActions_Code",
                table: "ShipmentActions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRoles_BillOfLadingId_ClientId_Role",
                table: "ShipmentRoles",
                columns: new[] { "BillOfLadingId", "ClientId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRoles_ClientId",
                table: "ShipmentRoles",
                column: "ClientId");

            // Usuarios existentes de organizaciones cliente: administran su organización (M1-02).
            migrationBuilder.Sql("""
                INSERT INTO "UserRoles" ("Id", "RoleName", "RoleId", "UserId")
                SELECT gen_random_uuid(), 'OrgAdmin', r."Id", u."Id"
                FROM "Users" u
                JOIN "Clients" c ON c."Id" = u."ClientId"
                LEFT JOIN "Roles" r ON r."Code" = 'OrgAdmin'
                WHERE c."OrganizationType" <> 'Internal'
                  AND u."MembershipStatus" = 'Active'
                  AND u."DeletedAt" IS NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM "UserRoles" ur
                      WHERE ur."UserId" = u."Id"
                        AND ur."RoleName" IN ('OrgAdmin', 'OrgOperator', 'OrgViewer'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationDocuments");

            migrationBuilder.DropTable(
                name: "ShipmentAccessRules");

            migrationBuilder.DropTable(
                name: "ShipmentRoles");

            migrationBuilder.DropTable(
                name: "ShipmentActions");

            migrationBuilder.DropIndex(
                name: "IX_Users_ClientId_MembershipStatus",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Clients_MatchCode",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_RegistrationStatus",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_BillsOfLading_BookingNumber",
                table: "BillsOfLading");

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000008"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000009"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000010"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000009"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000010"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000011"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000012"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("579700e8-1338-3738-63e6-8e8b8b7cad71"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("59a9ccfd-f3a1-90b7-b694-bd9eafb81228"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("920581cb-9857-6ad8-3406-9f8f54fd25fc"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c474e6b8-5abc-1ffc-bf2a-eeb14f3dd7a7"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5930f642-9936-0c5d-e985-2746431ae506"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("9aad4ae3-e43b-2fb4-92a5-bb181af7f9f4"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("bb9d489a-77a2-b2d6-c0f6-2df2ff63a1ff"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("f5904c22-d3b8-747e-062e-48c5d83ccc9d"));

            migrationBuilder.DeleteData(
                table: "ServiceOrders",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0010-0010-0010-000000000004"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("1c70376e-431c-3994-dd5c-464e0f1405f9"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("3c47e0d2-5bc6-59f8-dc0c-12f7eb10e017"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("3f8e776c-1c97-8168-2886-4ab337a4749e"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("a87fd524-45a0-5b06-c5bc-b848addc0849"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e2805dbb-3327-1b7d-aecd-7165a75c5d82"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000012"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000006"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000007"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000008"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("208dfafe-5331-55c8-f20c-1fedbb16b3cb"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("2477f174-66b3-8275-4ef4-b84902cdbb81"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d7f60fcb-88d4-54d3-54fb-d63def5c9613"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("227e87c5-a8d9-51c6-e0e9-373dc6d2c60b"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a084c0fb-db38-005b-3d5f-926ed28ed6df"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000011"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000050"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000040"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000050"));

            migrationBuilder.DropColumn(
                name: "MembershipDecidedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MembershipDecidedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MembershipStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ArCheckedAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ArCheckedBy",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ArReference",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "MatchCode",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "OperatingCountries",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "OrganizationType",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RegistrationStatus",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ValidatedAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ValidatedBy",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "BookingNumber",
                table: "BillsOfLading");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClientId",
                table: "Users",
                column: "ClientId");
        }
    }
}
