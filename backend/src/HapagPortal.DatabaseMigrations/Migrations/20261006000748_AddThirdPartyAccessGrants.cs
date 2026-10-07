using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddThirdPartyAccessGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ShipmentAccessRules",
                keyColumn: "Id",
                keyValue: new Guid("3072d047-9ee7-d892-5ff6-4adf7ecd0b54"));

            migrationBuilder.AddColumn<Guid>(
                name: "AccessGrantId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OnBehalfOfClientId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BookingNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccessGrantId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisibilityWideningId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantorClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    GranteeClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ActorClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantorClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantorRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GranteeClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookingNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GrantType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IntendedRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ActionCodes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CeilingActionCodes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ValidityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentGrantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultGranteeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsMandate = table.Column<bool>(type: "boolean", nullable: false),
                    TermsVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TermsAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TermsAcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessGrants_AccessGrants_ParentGrantId",
                        column: x => x.ParentGrantId,
                        principalTable: "AccessGrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_Clients_GranteeClientId",
                        column: x => x.GranteeClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_Clients_GrantorClientId",
                        column: x => x.GrantorClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefaultGrantees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantorClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    GranteeClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionCodes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_DefaultGrantees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefaultGrantees_Clients_GranteeClientId",
                        column: x => x.GranteeClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefaultGrantees_Clients_GrantorClientId",
                        column: x => x.GrantorClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpenAccessSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ActionCodes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenAccessSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenAccessSettings_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentAssociations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentAssociations_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAssociations_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VisibilityWidenings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantorClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantorRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginGrantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisibilityWidenings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisibilityWidenings_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisibilityWidenings_Clients_GrantorClientId",
                        column: x => x.GrantorClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AccessAuditEntries",
                columns: new[] { "Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId" },
                values: new object[,]
                {
                    { new Guid("cccccccc-0012-0012-0012-000000000031"), new Guid("cccccccc-0012-0012-0012-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("11111111-0007-0007-0007-000000000001"), "HLCUVAL250100123", "HLCUBKG2501001", "{\"grantType\":\"Individual\",\"source\":\"seed\"}", "GrantCreated", new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("cccccccc-0012-0012-0012-000000000032"), new Guid("cccccccc-0012-0012-0012-000000000002"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("11111111-0007-0007-0007-000000000002"), "HLCUVAL250200456", "HLCUBKG2502004", "{\"grantType\":\"Individual\",\"isMandate\":true,\"source\":\"seed\"}", "GrantCreated", new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("cccccccc-0012-0012-0012-000000000033"), new Guid("cccccccc-0012-0012-0012-000000000002"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("11111111-0007-0007-0007-000000000002"), "HLCUVAL250200456", "HLCUBKG2502004", "{\"termsVersion\":\"MANDATO-2026-10\"}", "MandateTermsAccepted", new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("cccccccc-0012-0012-0012-000000000034"), null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, null, "{\"durationDays\":180,\"source\":\"seed\"}", "DefaultGranteeAdded", new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("cccccccc-0012-0012-0012-000000000035"), null, null, "seed", null, null, null, null, "{\"isEnabled\":true,\"source\":\"seed\"}", "OpenAccessEnabled", null, new Guid("c3d4e5f6-0003-0003-0003-000000000040"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "AccessGrants",
                columns: new[] { "Id", "ActionCodes", "BillOfLadingId", "BookingNumber", "CeilingActionCodes", "CreatedAt", "CreatedBy", "DefaultGranteeId", "DeletedAt", "DeletedBy", "DurationDays", "EndReason", "EndedAt", "EndedByUserId", "GrantType", "GrantedByUserId", "GranteeClientId", "GrantorClientId", "GrantorRole", "IntendedRole", "IsMandate", "ModifiedAt", "ModifiedBy", "ParentGrantId", "Status", "TermsAcceptedAt", "TermsAcceptedByUserId", "TermsVersion", "ValidFrom", "ValidTo", "ValidityType" },
                values: new object[,]
                {
                    { new Guid("cccccccc-0012-0012-0012-000000000001"), "bl-issuance.view,import-demurrage.pay,local-charges-mandatory.pay,release-requirements.view,shipment.view,tracking.view", new Guid("11111111-0007-0007-0007-000000000001"), "HLCUBKG2501001", "access-audit.view,access-validity.set,access.grant,access.revoke,account-statement.view,bl-copy-unvalued.request,bl-copy-valued.request,bl-issuance.view,data-visibility.extend,drop-off.request,early-booking-access.grant,freight-certificate.generate,freight.pay,import-demurrage.pay,import-depot.view,invoices-billed.view,local-charges-mandatory.pay,local-charges-on-demand.pay,no-debt-certificate.download,open-access.enable,release-letter.generate,release-requirements.view,shipment.view,tatc.download,third-party-query.notify,tracking.view,transshipment-certificate.generate,warehouse-change.request", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, null, null, null, "Individual", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Customer", null, false, null, null, null, "Active", null, null, null, new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 3, 31, 23, 59, 0, 0, DateTimeKind.Utc), "UntilDate" },
                    { new Guid("cccccccc-0012-0012-0012-000000000002"), "freight.pay,local-charges-mandatory.pay,shipment.view", new Guid("11111111-0007-0007-0007-000000000002"), "HLCUBKG2502004", "access-audit.view,access-validity.set,access.grant,access.revoke,account-statement.view,bl-copy-unvalued.request,bl-copy-valued.request,bl-issuance.view,data-visibility.extend,drop-off.request,early-booking-access.grant,freight-certificate.generate,freight.pay,import-demurrage.pay,import-depot.view,invoices-billed.view,local-charges-mandatory.pay,local-charges-on-demand.pay,no-debt-certificate.download,open-access.enable,release-letter.generate,release-requirements.view,shipment.view,tatc.download,third-party-query.notify,tracking.view,transshipment-certificate.generate,warehouse-change.request", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, null, null, null, "Individual", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Customer", null, true, null, null, null, "Active", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "MANDATO-2026-10", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 3, 31, 23, 59, 0, 0, DateTimeKind.Utc), "UntilDate" }
                });

            migrationBuilder.InsertData(
                table: "DefaultGrantees",
                columns: new[] { "Id", "ActionCodes", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DurationDays", "GranteeClientId", "GrantorClientId", "IsActive", "ModifiedAt", "ModifiedBy" },
                values: new object[] { new Guid("cccccccc-0012-0012-0012-000000000011"), null, new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 180, new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), true, null, null });

            migrationBuilder.InsertData(
                table: "OpenAccessSettings",
                columns: new[] { "Id", "ActionCodes", "ChangedAt", "ChangedByUserId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy" },
                values: new object[] { new Guid("cccccccc-0012-0012-0012-000000000021"), "bl-issuance.view,local-charges-mandatory.pay,release-requirements.view,shipment.view,tracking.view", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), null, new Guid("c3d4e5f6-0003-0003-0003-000000000040"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000001"),
                columns: new[] { "AccessGrantId", "OnBehalfOfClientId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000002"),
                columns: new[] { "AccessGrantId", "OnBehalfOfClientId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000003"),
                columns: new[] { "AccessGrantId", "OnBehalfOfClientId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000004"),
                columns: new[] { "AccessGrantId", "OnBehalfOfClientId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000005"),
                columns: new[] { "AccessGrantId", "OnBehalfOfClientId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000006"),
                columns: new[] { "AccessGrantId", "OnBehalfOfClientId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000007"),
                columns: new[] { "AccessGrantId", "OnBehalfOfClientId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000008"),
                columns: new[] { "AccessGrantId", "OnBehalfOfClientId" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[] { new Guid("ddb82640-b032-a4fd-8e31-17c5f6b0ca8a"), "org.access.manage", "org.access.manage" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[] { new Guid("fd185e2b-35f8-cf89-f39c-5d8158027c0a"), new Guid("ddb82640-b032-a4fd-8e31-17c5f6b0ca8a"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c") });

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditEntries_AccessGrantId",
                table: "AccessAuditEntries",
                column: "AccessGrantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditEntries_BillOfLadingId_OccurredAt",
                table: "AccessAuditEntries",
                columns: new[] { "BillOfLadingId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditEntries_BookingNumber",
                table: "AccessAuditEntries",
                column: "BookingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditEntries_GranteeClientId",
                table: "AccessAuditEntries",
                column: "GranteeClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditEntries_GrantorClientId",
                table: "AccessAuditEntries",
                column: "GrantorClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_BillOfLadingId_GrantorClientId_GranteeClientId",
                table: "AccessGrants",
                columns: new[] { "BillOfLadingId", "GrantorClientId", "GranteeClientId" },
                unique: true,
                filter: "\"Status\" IN ('Active', 'PendingAcceptance') AND \"BillOfLadingId\" IS NOT NULL AND \"GrantType\" <> 'EarlyBooking'");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_BookingNumber",
                table: "AccessGrants",
                column: "BookingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_GranteeClientId_Status_ValidTo",
                table: "AccessGrants",
                columns: new[] { "GranteeClientId", "Status", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_GrantorClientId_Status",
                table: "AccessGrants",
                columns: new[] { "GrantorClientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_ParentGrantId",
                table: "AccessGrants",
                column: "ParentGrantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_Status_ValidTo",
                table: "AccessGrants",
                columns: new[] { "Status", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_DefaultGrantees_GranteeClientId",
                table: "DefaultGrantees",
                column: "GranteeClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DefaultGrantees_GrantorClientId_GranteeClientId",
                table: "DefaultGrantees",
                columns: new[] { "GrantorClientId", "GranteeClientId" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_OpenAccessSettings_ClientId",
                table: "OpenAccessSettings",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAssociations_BillOfLadingId_ClientId",
                table: "ShipmentAssociations",
                columns: new[] { "BillOfLadingId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAssociations_ClientId",
                table: "ShipmentAssociations",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_VisibilityWidenings_BillOfLadingId_TargetRole_Status",
                table: "VisibilityWidenings",
                columns: new[] { "BillOfLadingId", "TargetRole", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VisibilityWidenings_GrantorClientId",
                table: "VisibilityWidenings",
                column: "GrantorClientId");

            migrationBuilder.CreateIndex(
                name: "IX_VisibilityWidenings_OriginGrantId",
                table: "VisibilityWidenings",
                column: "OriginGrantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessAuditEntries");

            migrationBuilder.DropTable(
                name: "AccessGrants");

            migrationBuilder.DropTable(
                name: "DefaultGrantees");

            migrationBuilder.DropTable(
                name: "OpenAccessSettings");

            migrationBuilder.DropTable(
                name: "ShipmentAssociations");

            migrationBuilder.DropTable(
                name: "VisibilityWidenings");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("fd185e2b-35f8-cf89-f39c-5d8158027c0a"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("ddb82640-b032-a4fd-8e31-17c5f6b0ca8a"));

            migrationBuilder.DropColumn(
                name: "AccessGrantId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OnBehalfOfClientId",
                table: "Payments");

            migrationBuilder.InsertData(
                table: "ShipmentAccessRules",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId" },
                values: new object[] { new Guid("3072d047-9ee7-d892-5ff6-4adf7ecd0b54"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Allowed", null, null, "FreightForwarder", "ThirdParty", new Guid("a2d100d7-15e0-44de-45a3-3d732cef7332") });
        }
    }
}
