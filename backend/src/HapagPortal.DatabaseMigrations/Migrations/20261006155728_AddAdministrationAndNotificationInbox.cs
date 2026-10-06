using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrationAndNotificationInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActionResolvedAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionTargetId",
                table: "Notifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "Notifications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlNumber",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailSent",
                table: "Notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "Notifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityReference",
                table: "Notifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Notifications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "Notifications",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleEs = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BodyEs = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    BodyEn = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Countries = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Operation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UnpublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnpublishedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NotifyOnPublish = table.Column<bool>(type: "boolean", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarrierPreRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvitationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierPreRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactListChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PreviousEmails = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    NewEmails = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactListChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CounterRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ExchangeDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HblReceived = table.Column<bool>(type: "boolean", nullable: false),
                    HblReceivedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Deconsolidated = table.Column<bool>(type: "boolean", nullable: false),
                    DeconsolidatedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SyncStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncError = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CounterRecords_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuideDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    NameEs = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DescriptionEs = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    StepsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImpersonationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    RequestCount = table.Column<int>(type: "integer", nullable: false),
                    BlockedCount = table.Column<int>(type: "integer", nullable: false),
                    SourceAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpersonationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationParentLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VisibilityEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    VisibilityChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VisibilityChangedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DecisionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationParentLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGuideStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuideCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GuideVersion = table.Column<int>(type: "integer", nullable: false),
                    LastStep = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGuideStates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AccessAuditEntries",
                columns: new[] { "Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId" },
                values: new object[,]
                {
                    { new Guid("05e01777-b203-ab12-9d17-52be09a3061e"), null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, null, "{\"preRegistrationId\":\"ffffffff-0025-0025-0025-000000000001\",\"created\":true,\"source\":\"seed\"}", "CarrierPreCreated", new Guid("c3d4e5f6-0003-0003-0003-000000000091"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("0be019c6-b464-f45c-6eee-4541493e9060"), new Guid("cccccccc-0012-0012-0012-000000000003"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("11111111-0007-0007-0007-000000000002"), "HLCUVAL250200456", "HLCUBKG2502004", "{\"grantType\":\"Individual\",\"preCreatedCarrier\":true,\"status\":\"PendingActivation\",\"durationDays\":90,\"source\":\"seed\"}", "GrantCreated", new Guid("c3d4e5f6-0003-0003-0003-000000000091"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("25a03bf1-6107-d007-e1bc-24be1b53f3be"), null, new Guid("c3d4e5f6-0003-0003-0003-000000000001"), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), null, null, null, "{\"parentLinkId\":\"ffffffff-0026-0026-0026-000000000001\",\"source\":\"seed\"}", "ParentLinkApproved", new Guid("c3d4e5f6-0003-0003-0003-000000000090"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("5cb9eac8-2fcd-a6f8-82a7-fb5125b6dc84"), null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, null, "{\"parentLinkId\":\"ffffffff-0026-0026-0026-000000000001\",\"visibilityEnabled\":true,\"source\":\"seed\"}", "ParentLinkRequested", new Guid("c3d4e5f6-0003-0003-0003-000000000090"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("6f216ae0-209c-c092-4099-0043c21b3aa8"), null, new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), null, null, null, "{\"parentLinkId\":\"ffffffff-0026-0026-0026-000000000002\",\"visibilityEnabled\":true,\"source\":\"seed\"}", "ParentLinkRequested", new Guid("c3d4e5f6-0003-0003-0003-000000000090"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "Announcements",
                columns: new[] { "Id", "BodyEn", "BodyEs", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "NotifiedAt", "NotifyOnPublish", "Operation", "PublishedAt", "PublishedBy", "Severity", "Status", "TitleEn", "TitleEs", "UnpublishedAt", "UnpublishedBy", "ValidFrom", "ValidTo" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0027-0027-0027-000000000001"), "From October 1st the San Antonio empty depot receives returns Monday to Saturday from 08:00 to 20:00. Schedule the Gate In in advance to avoid delays.", "Desde el 1 de octubre el depósito de vacíos de San Antonio recibe devoluciones de lunes a sábado de 08:00 a 20:00. Programe el Gate In con anticipación para evitar demoras.", "CL", new DateTime(2026, 10, 1, 11, 30, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", null, null, null, null, null, false, "Import", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", "Important", "Published", "New opening hours at the San Antonio empty depot", "Nuevo horario del depósito de vacíos en San Antonio", null, null, new DateTime(2026, 10, 1, 3, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 1, 3, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ffffffff-0027-0027-0027-000000000002"), "Bolivian export BLs are exchanged at the Arica Counter presenting the original BL and the release letter. The exchange status is shown in the shipment detail.", "El canje de los BL de exportación de Bolivia se realiza en el Counter de Arica presentando el BL original y la carta de liberación. El estado del canje queda visible en el detalle del embarque.", "BO", new DateTime(2026, 10, 2, 12, 45, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", null, null, null, null, null, false, "Export", new DateTime(2026, 10, 2, 13, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", "Info", "Published", "Bolivian export BL exchange in Arica", "Canje de BL de exportación boliviana en Arica", null, null, new DateTime(2026, 10, 2, 4, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("ffffffff-0027-0027-0027-000000000003"), "The portal will be unavailable on October 20th between 23:00 and 23:59 (Chile time) for scheduled maintenance.", "El portal no estará disponible el 20 de octubre entre las 23:00 y las 23:59 (hora de Chile) por mantención programada.", "CL,BO", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", null, null, null, null, null, true, "Both", null, null, "Important", "Draft", "Scheduled portal maintenance", "Mantención programada del portal", null, null, new DateTime(2026, 10, 15, 3, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 21, 3, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "AuditLogs",
                columns: new[] { "Id", "Action", "EntityId", "EntityName", "NewValues", "OldValues", "Timestamp", "UserId" },
                values: new object[,]
                {
                    { new Guid("044990a0-297c-6041-7f72-96fd33ff3f5f"), "Ended", "ffffffff-0030-0030-0030-000000000001", "ImpersonationSession", "{\"actorUserId\":\"d4e5f6a7-0004-0004-0004-000000000001\",\"actorEmail\":\"admin@hapag-lloyd.cl\",\"subjectUserId\":\"d4e5f6a7-0004-0004-0004-000000000010\",\"subjectEmail\":\"demo@importadorademo.cl\",\"organizationId\":\"c3d4e5f6-0003-0003-0003-000000000010\",\"organizationName\":\"Importadora Demo SpA\",\"details\":{\"reason\":\"Manual\",\"durationSeconds\":720,\"requestCount\":6,\"blockedCount\":1}}", null, new DateTime(2026, 10, 4, 15, 12, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000001" },
                    { new Guid("8dd50263-2800-0557-93b9-07a71d223c9b"), "BlockedWrite", "ffffffff-0030-0030-0030-000000000001", "ImpersonationSession", "{\"actorUserId\":\"d4e5f6a7-0004-0004-0004-000000000001\",\"actorEmail\":\"admin@hapag-lloyd.cl\",\"subjectUserId\":\"d4e5f6a7-0004-0004-0004-000000000010\",\"subjectEmail\":\"demo@importadorademo.cl\",\"organizationId\":\"c3d4e5f6-0003-0003-0003-000000000010\",\"organizationName\":\"Importadora Demo SpA\",\"details\":{\"method\":\"POST\",\"path\":\"/api/v1/cart/items\",\"statusCode\":403}}", null, new DateTime(2026, 10, 4, 15, 7, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000001" },
                    { new Guid("b92706ba-8ccb-e7ef-266a-5ecab8db678a"), "Started", "ffffffff-0030-0030-0030-000000000001", "ImpersonationSession", "{\"actorUserId\":\"d4e5f6a7-0004-0004-0004-000000000001\",\"actorEmail\":\"admin@hapag-lloyd.cl\",\"subjectUserId\":\"d4e5f6a7-0004-0004-0004-000000000010\",\"subjectEmail\":\"demo@importadorademo.cl\",\"organizationId\":\"c3d4e5f6-0003-0003-0003-000000000010\",\"organizationName\":\"Importadora Demo SpA\",\"details\":{\"reason\":\"Ticket CS-2026-1004\",\"readOnly\":true,\"allowedActions\":[]}}", null, new DateTime(2026, 10, 4, 15, 0, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000001" }
                });

            migrationBuilder.InsertData(
                table: "CarrierPreRegistrations",
                columns: new[] { "Id", "ActivatedAt", "CarrierOrganizationId", "CarrierUserId", "Country", "CreatedAt", "DurationDays", "Email", "InvitationSentAt", "LegalName", "RequestedBy", "RequestedByOrganizationId", "RequestedByUserId", "Status", "TaxId" },
                values: new object[] { new Guid("ffffffff-0025-0025-0025-000000000001"), null, new Guid("c3d4e5f6-0003-0003-0003-000000000091"), new Guid("d4e5f6a7-0004-0004-0004-000000000091"), "CL", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), 90, "contacto@transportescordillera.cl", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "Transportes Cordillera Ltda.", "demo@importadorademo.cl", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Pending", "77.123.321-5" });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy" },
                values: new object[,]
                {
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000090"), null, null, new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Client", "CL", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "holding@grupodemo.cl", true, true, "MC100090", null, null, "Grupo Demo Holding S.A.", "CL,BO", "Customer", "+56 2 2400 1000", "Approved", null, null, "96.700.100-1", "RUT", null, null },
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000091"), null, null, null, null, null, null, null, "Client", "CL", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "contacto@transportescordillera.cl", true, false, null, null, null, "Transportes Cordillera Ltda.", "CL", "Carrier", null, "PreCreated", null, "Pre-creado por Importadora Demo SpA (M1-09).", "77.123.321-5", "RUT", null, null }
                });

            migrationBuilder.InsertData(
                table: "ContactListChanges",
                columns: new[] { "Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "ErrorCode", "NewEmails", "OrganizationId", "PreviousEmails", "ReportType", "SourceReference", "Status" },
                values: new object[] { new Guid("ffffffff-0031-0031-0031-000000000001"), new DateTime(2026, 9, 15, 13, 0, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, "finanzas@importadorademo.cl", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "contabilidad@importadorademo.cl", "INVOICES", "P0060-SEED0001", "Propagated" });

            migrationBuilder.InsertData(
                table: "CounterRecords",
                columns: new[] { "Id", "BillOfLadingId", "BlNumber", "Country", "CreatedAt", "CreatedBy", "Deconsolidated", "DeconsolidatedAt", "DeletedAt", "DeletedBy", "ExchangeDate", "HblReceived", "HblReceivedAt", "ModifiedAt", "ModifiedBy", "Notes", "RecordedAt", "RecordedBy", "RecordedByUserId", "SourceReference", "SyncError", "SyncStatus", "SyncedAt" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0029-0029-0029-000000000001"), new Guid("11111111-0007-0007-0007-000000000008"), "HLCUARI260300830", "BO", new DateTime(2026, 10, 3, 14, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", false, null, null, null, new DateOnly(2026, 10, 2), true, new DateOnly(2026, 10, 3), null, null, null, new DateTime(2026, 10, 3, 14, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), "CNT-BO-000830", null, "Synced", new DateTime(2026, 10, 3, 14, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ffffffff-0029-0029-0029-000000000002"), new Guid("11111111-0007-0007-0007-000000000004"), "HLCUARI260100045", "BO", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", true, new DateOnly(2026, 10, 5), null, null, new DateOnly(2026, 10, 4), true, new DateOnly(2026, 10, 4), null, null, "Desconsolidado en el depósito de Arica.", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), null, "Integration.Unavailable", "Failed", null }
                });

            migrationBuilder.InsertData(
                table: "GuideDefinitions",
                columns: new[] { "Id", "Audience", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Route", "StepsJson", "Version" },
                values: new object[] { new Guid("ffffffff-0028-0028-0028-000000000001"), "Client", "cart-checkout", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Walks through the unified cart: added charges, billing tax ID, currency, payment method and confirmation.", "Recorre el carro unificado: cargos agregados, RUT de facturación, moneda, medio de pago y confirmación.", 10, true, null, null, "How to pay from the cart", "Cómo pagar desde el carro", "/cart", "[{\"order\":1,\"route\":\"/cart\",\"elementKey\":\"cart.items\",\"titleEs\":\"Revise los cargos\",\"titleEn\":\"Review the charges\",\"textEs\":\"Aqu\\u00ED est\\u00E1n los cargos que agreg\\u00F3 desde sus embarques, agrupados por moneda. Puede quitar los que no pagar\\u00E1 ahora.\",\"textEn\":\"These are the charges you added from your shipments, grouped by currency. You can remove the ones you will not pay now.\"},{\"order\":2,\"route\":\"/cart\",\"elementKey\":\"cart.billing-tax-id\",\"titleEs\":\"RUT de facturaci\\u00F3n\",\"titleEn\":\"Billing tax ID\",\"textEs\":\"Indique a qu\\u00E9 RUT se emitir\\u00E1 la factura de cada cargo. Por defecto es el de su organizaci\\u00F3n.\",\"textEn\":\"Choose the tax ID to be invoiced for each charge. By default it is your organization\\u0027s.\"},{\"order\":3,\"route\":\"/cart\",\"elementKey\":\"cart.currency\",\"titleEs\":\"Moneda de pago\",\"titleEn\":\"Payment currency\",\"textEs\":\"Elija la moneda en que pagar\\u00E1; si es distinta de la del cargo se usa el tipo de cambio del d\\u00EDa.\",\"textEn\":\"Choose the payment currency; if it differs from the charge currency the day\\u0027s exchange rate applies.\"},{\"order\":4,\"route\":\"/cart\",\"elementKey\":\"cart.payment-method\",\"titleEs\":\"Medio de pago\",\"titleEn\":\"Payment method\",\"textEs\":\"Seleccione el medio de pago habilitado para su pa\\u00EDs y moneda.\",\"textEn\":\"Select a payment method enabled for your country and currency.\"},{\"order\":5,\"route\":\"/cart\",\"elementKey\":\"cart.checkout\",\"titleEs\":\"Pague\",\"titleEn\":\"Pay\",\"textEs\":\"Confirme el pago. Recibir\\u00E1 el comprobante en la bandeja de notificaciones y en el historial de pagos.\",\"textEn\":\"Confirm the payment. You will receive the receipt in the notification inbox and in the payment history.\"}]", 1 });

            migrationBuilder.InsertData(
                table: "ImpersonationSessions",
                columns: new[] { "Id", "ActorEmail", "ActorUserId", "BlockedCount", "DurationSeconds", "EndReason", "EndedAt", "ExpiresAt", "OrganizationId", "OrganizationName", "Reason", "RequestCount", "SourceAddress", "StartedAt", "Status", "SubjectEmail", "SubjectUserId" },
                values: new object[] { new Guid("ffffffff-0030-0030-0030-000000000001"), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), 1, 720, "Manual", new DateTime(2026, 10, 4, 15, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 4, 15, 30, 0, 0, DateTimeKind.Utc), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "Ticket CS-2026-1004: el cliente no ve el flete del BL HLCUVAL250200456.", 6, "10.0.0.15", new DateTime(2026, 10, 4, 15, 0, 0, 0, DateTimeKind.Utc), "Ended", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010") });

            migrationBuilder.InsertData(
                table: "MaintainerChangeLogs",
                columns: new[] { "Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue" },
                values: new object[,]
                {
                    { new Guid("0f35358b-a740-0f03-384d-771138ac802d"), "Created", new DateTime(2026, 10, 3, 14, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), new Guid("ffffffff-0029-0029-0029-000000000001"), "CounterRecord", "{\"blNumber\":\"HLCUARI260300830\",\"country\":\"BO\",\"exchangeDate\":\"2026-10-02\",\"hblReceived\":true,\"hblReceivedAt\":\"2026-10-03\",\"deconsolidated\":false,\"deconsolidatedAt\":null,\"notes\":null,\"syncStatus\":\"Pending\",\"sourceReference\":null}", null },
                    { new Guid("293debcc-96de-bc6a-7028-c71f703ef980"), "Created", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0028-0028-0028-000000000001"), "Guide", "{\"code\":\"cart-checkout\",\"nameEs\":\"C\\u00F3mo pagar desde el carro\",\"nameEn\":\"How to pay from the cart\",\"route\":\"/cart\",\"audience\":\"Client\",\"isActive\":true,\"displayOrder\":10,\"version\":1,\"steps\":[{\"order\":1,\"route\":\"/cart\",\"elementKey\":\"cart.items\",\"titleEs\":\"Revise los cargos\",\"titleEn\":\"Review the charges\",\"textEs\":\"Aqu\\u00ED est\\u00E1n los cargos que agreg\\u00F3 desde sus embarques, agrupados por moneda. Puede quitar los que no pagar\\u00E1 ahora.\",\"textEn\":\"These are the charges you added from your shipments, grouped by currency. You can remove the ones you will not pay now.\"},{\"order\":2,\"route\":\"/cart\",\"elementKey\":\"cart.billing-tax-id\",\"titleEs\":\"RUT de facturaci\\u00F3n\",\"titleEn\":\"Billing tax ID\",\"textEs\":\"Indique a qu\\u00E9 RUT se emitir\\u00E1 la factura de cada cargo. Por defecto es el de su organizaci\\u00F3n.\",\"textEn\":\"Choose the tax ID to be invoiced for each charge. By default it is your organization\\u0027s.\"},{\"order\":3,\"route\":\"/cart\",\"elementKey\":\"cart.currency\",\"titleEs\":\"Moneda de pago\",\"titleEn\":\"Payment currency\",\"textEs\":\"Elija la moneda en que pagar\\u00E1; si es distinta de la del cargo se usa el tipo de cambio del d\\u00EDa.\",\"textEn\":\"Choose the payment currency; if it differs from the charge currency the day\\u0027s exchange rate applies.\"},{\"order\":4,\"route\":\"/cart\",\"elementKey\":\"cart.payment-method\",\"titleEs\":\"Medio de pago\",\"titleEn\":\"Payment method\",\"textEs\":\"Seleccione el medio de pago habilitado para su pa\\u00EDs y moneda.\",\"textEn\":\"Select a payment method enabled for your country and currency.\"},{\"order\":5,\"route\":\"/cart\",\"elementKey\":\"cart.checkout\",\"titleEs\":\"Pague\",\"titleEn\":\"Pay\",\"textEs\":\"Confirme el pago. Recibir\\u00E1 el comprobante en la bandeja de notificaciones y en el historial de pagos.\",\"textEn\":\"Confirm the payment. You will receive the receipt in the notification inbox and in the payment history.\"}]}", null },
                    { new Guid("3e4a98bd-4067-59ac-66e0-6e5cb4137108"), "Created", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), new Guid("ffffffff-0027-0027-0027-000000000003"), "Announcement", "{\"titleEs\":\"Mantenci\\u00F3n programada del portal\",\"titleEn\":\"Scheduled portal maintenance\",\"bodyEs\":\"El portal no estar\\u00E1 disponible el 20 de octubre entre las 23:00 y las 23:59 (hora de Chile) por mantenci\\u00F3n programada.\",\"bodyEn\":\"The portal will be unavailable on October 20th between 23:00 and 23:59 (Chile time) for scheduled maintenance.\",\"countries\":\"CL,BO\",\"operation\":\"Both\",\"severity\":\"Important\",\"validFrom\":\"2026-10-15T03:00:00Z\",\"validTo\":\"2026-10-21T03:00:00Z\",\"status\":\"Draft\",\"notifyOnPublish\":true}", null },
                    { new Guid("a32f168f-b981-c833-76f9-e98191ff9d3f"), "Created", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), new Guid("ffffffff-0029-0029-0029-000000000002"), "CounterRecord", "{\"blNumber\":\"HLCUARI260100045\",\"country\":\"BO\",\"exchangeDate\":\"2026-10-04\",\"hblReceived\":true,\"hblReceivedAt\":\"2026-10-04\",\"deconsolidated\":true,\"deconsolidatedAt\":\"2026-10-05\",\"notes\":\"Desconsolidado en el dep\\u00F3sito de Arica.\",\"syncStatus\":\"Pending\",\"sourceReference\":null}", null },
                    { new Guid("b0e7ddaa-2c3c-3d6f-e8fc-a47e3dc3fe71"), "Created", new DateTime(2026, 10, 1, 11, 30, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), new Guid("ffffffff-0027-0027-0027-000000000001"), "Announcement", "{\"titleEs\":\"Nuevo horario del dep\\u00F3sito de vac\\u00EDos en San Antonio\",\"titleEn\":\"New opening hours at the San Antonio empty depot\",\"bodyEs\":\"Desde el 1 de octubre el dep\\u00F3sito de vac\\u00EDos de San Antonio recibe devoluciones de lunes a s\\u00E1bado de 08:00 a 20:00. Programe el Gate In con anticipaci\\u00F3n para evitar demoras.\",\"bodyEn\":\"From October 1st the San Antonio empty depot receives returns Monday to Saturday from 08:00 to 20:00. Schedule the Gate In in advance to avoid delays.\",\"countries\":\"CL\",\"operation\":\"Import\",\"severity\":\"Important\",\"validFrom\":\"2026-10-01T03:00:00Z\",\"validTo\":\"2027-01-01T03:00:00Z\",\"status\":\"Published\",\"notifyOnPublish\":false}", null },
                    { new Guid("e5e93f5e-8734-6c2f-751d-09b56c091735"), "Created", new DateTime(2026, 10, 2, 12, 45, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), new Guid("ffffffff-0027-0027-0027-000000000002"), "Announcement", "{\"titleEs\":\"Canje de BL de exportaci\\u00F3n boliviana en Arica\",\"titleEn\":\"Bolivian export BL exchange in Arica\",\"bodyEs\":\"El canje de los BL de exportaci\\u00F3n de Bolivia se realiza en el Counter de Arica presentando el BL original y la carta de liberaci\\u00F3n. El estado del canje queda visible en el detalle del embarque.\",\"bodyEn\":\"Bolivian export BLs are exchanged at the Arica Counter presenting the original BL and the release letter. The exchange status is shown in the shipment detail.\",\"countries\":\"BO\",\"operation\":\"Export\",\"severity\":\"Info\",\"validFrom\":\"2026-10-02T04:00:00Z\",\"validTo\":null,\"status\":\"Published\",\"notifyOnPublish\":false}", null }
                });

            migrationBuilder.InsertData(
                table: "NotificationPreferences",
                columns: new[] { "Id", "EmailEnabled", "NotificationType", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("4728100f-24b0-d70d-8491-616cbedbcff8"), false, "DocumentIssued", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("d4e5f6a7-0004-0004-0004-000000000010") },
                    { new Guid("85b8a6da-44d9-7264-e037-62ac5754f081"), true, "AnnouncementPublished", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("d4e5f6a7-0004-0004-0004-000000000010") }
                });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "ActionResolvedAt", "ActionTargetId", "ActionType", "BlNumber", "Body", "CreatedAt", "CreatedBy", "DedupKey", "DeletedAt", "DeletedBy", "EmailSent", "EntityId", "EntityReference", "EntityType", "ModifiedAt", "ModifiedBy", "Module", "ReadAt", "RoleCode", "Title", "Type", "UserId" },
                values: new object[,]
                {
                    { new Guid("46a9dd3c-e522-7448-02ab-7696e37612eb"), null, null, null, null, "Importadora Demo SpA quedó asociada a su organización como filial. Ya puede ver sus BL en el listado de embarques, identificados por organización.", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, "ffffffff-0026-0026-0026-000000000001", "Importadora Demo SpA", "ParentLink", null, null, "Organization", null, null, "Filial asociada: Importadora Demo SpA", "ParentLinkApproved", new Guid("d4e5f6a7-0004-0004-0004-000000000090") },
                    { new Guid("be0c6821-74a8-fb28-c072-2e6dd368194f"), null, "ffffffff-0026-0026-0026-000000000002", "ReviewParentLink", null, "Comercial Altiplano SRL pide asociarse a Grupo Demo Holding S.A. como su empresa matriz. Revise la solicitud en el área de administración.", new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "parent-link-requested:ffffffff-0026-0026-0026-000000000002", null, null, false, "ffffffff-0026-0026-0026-000000000002", "Comercial Altiplano SRL", "ParentLink", null, null, "Administration", null, "Administrador", "Solicitud de empresa matriz: Comercial Altiplano SRL", "ParentLinkRequested", null },
                    { new Guid("d5e36d15-3543-4d73-b0fc-af18dbec0030"), null, "d4e5f6a7-0004-0004-0004-000000000012", "ApproveJoinRequest", null, "Sergio Solicitante (solicitud@importadorademo.cl) solicita vincularse a Importadora Demo SpA.", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "join-request:d4e5f6a7-0004-0004-0004-000000000012:d4e5f6a7-0004-0004-0004-000000000010", null, null, false, "d4e5f6a7-0004-0004-0004-000000000012", "Sergio Solicitante", "JoinRequest", null, null, "Organization", null, null, "Nueva solicitud de vinculación", "JoinRequestReceived", new Guid("d4e5f6a7-0004-0004-0004-000000000010") }
                });

            migrationBuilder.InsertData(
                table: "OrganizationParentLinks",
                columns: new[] { "Id", "DecidedAt", "DecidedBy", "DecisionNotes", "EndedAt", "EndedBy", "Notes", "OrganizationId", "ParentOrganizationId", "RequestedAt", "RequestedBy", "RequestedByUserId", "Status", "VisibilityChangedAt", "VisibilityChangedBy", "VisibilityEnabled" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0026-0026-0026-000000000001"), new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", "Escritura de constitución del grupo verificada.", null, null, "Importadora Demo es filial del grupo.", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new Guid("c3d4e5f6-0003-0003-0003-000000000090"), new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Active", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", true },
                    { new Guid("ffffffff-0026-0026-0026-000000000002"), null, null, null, null, null, "Filial boliviana del grupo.", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), new Guid("c3d4e5f6-0003-0003-0003-000000000090"), new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), "Pending", new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", true }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { new Guid("19a63320-87df-605a-721c-5740ad587879"), "counter.manage", "counter.manage" },
                    { new Guid("4d5a28c9-edc7-c3ae-e185-e86340b1787a"), "announcements.manage", "announcements.manage" },
                    { new Guid("83a1a2ee-e56f-cd4e-d5f5-47e46234fa75"), "admin-area.access", "admin-area.access" },
                    { new Guid("91d410af-6706-1491-a2c4-b06a18c8ec72"), "transactions-report.view", "transactions-report.view" },
                    { new Guid("fb1e2d86-80da-a74a-d175-d17668f16149"), "impersonation.use", "impersonation.use" }
                });

            migrationBuilder.InsertData(
                table: "AccessGrants",
                columns: new[] { "Id", "ActionCodes", "BillOfLadingId", "BookingNumber", "CeilingActionCodes", "CreatedAt", "CreatedBy", "DefaultGranteeId", "DeletedAt", "DeletedBy", "DurationDays", "EndReason", "EndedAt", "EndedByUserId", "GrantType", "GrantedByUserId", "GranteeClientId", "GrantorClientId", "GrantorRole", "IntendedRole", "IsMandate", "ModifiedAt", "ModifiedBy", "ParentGrantId", "Status", "TermsAcceptedAt", "TermsAcceptedByUserId", "TermsVersion", "ValidFrom", "ValidTo", "ValidityType" },
                values: new object[] { new Guid("cccccccc-0012-0012-0012-000000000003"), null, new Guid("11111111-0007-0007-0007-000000000002"), "HLCUBKG2502004", "access-audit.view,access-validity.set,access.grant,access.revoke,account-statement.view,bl-copy-unvalued.request,bl-copy-valued.request,bl-issuance.view,data-visibility.extend,drop-off.request,early-booking-access.grant,freight-certificate.generate,freight.pay,import-demurrage.pay,import-depot.view,invoices-billed.view,local-charges-mandatory.pay,local-charges-on-demand.pay,no-debt-certificate.download,open-access.enable,release-letter.generate,release-requirements.view,shipment.view,tatc.download,third-party-query.notify,tracking.view,transshipment-certificate.generate,warehouse-change.request", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, 90, null, null, null, "Individual", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("c3d4e5f6-0003-0003-0003-000000000091"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Customer", null, false, null, null, null, "PendingActivation", null, null, null, new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), null, "Duration" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("243b1584-1695-7ee6-9356-12ca13425aaf"), new Guid("83a1a2ee-e56f-cd4e-d5f5-47e46234fa75"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("8d7f6b98-d232-0652-9708-e433e7f80ba1"), new Guid("91d410af-6706-1491-a2c4-b06a18c8ec72"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("ce8e714c-3cbb-8ef0-770c-dd6d3a6377c8"), new Guid("83a1a2ee-e56f-cd4e-d5f5-47e46234fa75"), new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a") },
                    { new Guid("fd71bfe7-2fba-41ab-5548-ccdf4539f4df"), new Guid("19a63320-87df-605a-721c-5740ad587879"), new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a") }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username" },
                values: new object[,]
                {
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000090"), new Guid("c3d4e5f6-0003-0003-0003-000000000090"), "CL", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "holding@grupodemo.cl", null, null, "Hilda", true, false, null, "Holding", null, null, "Active", null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, null, "Client", "holding@grupodemo.cl" },
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000091"), new Guid("c3d4e5f6-0003-0003-0003-000000000091"), "CL", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "contacto@transportescordillera.cl", null, null, "Tomás", true, false, null, "Cordillera", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", "Active", null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", "CARRIER-DEMO-INVITE-2026-10", new DateTime(2026, 12, 31, 23, 59, 0, 0, DateTimeKind.Utc), null, null, null, "Client", "contacto@transportescordillera.cl" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "RoleId", "RoleName", "UserId" },
                values: new object[,]
                {
                    { new Guid("3cc0937d-04de-51c0-4c38-f3a240ebcd8a"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new Guid("d4e5f6a7-0004-0004-0004-000000000091") },
                    { new Guid("6e26ae2d-56bd-69c9-28fa-cc0c9c248a42"), new Guid("e200b49e-343b-36a4-fbcc-e10fa786728c"), "OrgAdmin", new Guid("d4e5f6a7-0004-0004-0004-000000000090") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActionType_ActionTargetId",
                table: "Notifications",
                columns: new[] { "ActionType", "ActionTargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_Status_ValidFrom",
                table: "Announcements",
                columns: new[] { "Status", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierPreRegistrations_CarrierOrganizationId_RequestedByOr~",
                table: "CarrierPreRegistrations",
                columns: new[] { "CarrierOrganizationId", "RequestedByOrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarrierPreRegistrations_RequestedByOrganizationId",
                table: "CarrierPreRegistrations",
                column: "RequestedByOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactListChanges_OrganizationId_ChangedAt",
                table: "ContactListChanges",
                columns: new[] { "OrganizationId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CounterRecords_BillOfLadingId",
                table: "CounterRecords",
                column: "BillOfLadingId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CounterRecords_BlNumber",
                table: "CounterRecords",
                column: "BlNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CounterRecords_SyncStatus",
                table: "CounterRecords",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_GuideDefinitions_Code",
                table: "GuideDefinitions",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_ActorUserId_Status",
                table: "ImpersonationSessions",
                columns: new[] { "ActorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_OrganizationId_StartedAt",
                table: "ImpersonationSessions",
                columns: new[] { "OrganizationId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_StartedAt",
                table: "ImpersonationSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_NotificationType",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "NotificationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationParentLinks_OrganizationId",
                table: "OrganizationParentLinks",
                column: "OrganizationId",
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Active')");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationParentLinks_ParentOrganizationId_Status",
                table: "OrganizationParentLinks",
                columns: new[] { "ParentOrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserGuideStates_UserId_GuideCode",
                table: "UserGuideStates",
                columns: new[] { "UserId", "GuideCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "CarrierPreRegistrations");

            migrationBuilder.DropTable(
                name: "ContactListChanges");

            migrationBuilder.DropTable(
                name: "CounterRecords");

            migrationBuilder.DropTable(
                name: "GuideDefinitions");

            migrationBuilder.DropTable(
                name: "ImpersonationSessions");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "OrganizationParentLinks");

            migrationBuilder.DropTable(
                name: "UserGuideStates");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ActionType_ActionTargetId",
                table: "Notifications");

            migrationBuilder.DeleteData(
                table: "AccessAuditEntries",
                keyColumn: "Id",
                keyValue: new Guid("05e01777-b203-ab12-9d17-52be09a3061e"));

            migrationBuilder.DeleteData(
                table: "AccessAuditEntries",
                keyColumn: "Id",
                keyValue: new Guid("0be019c6-b464-f45c-6eee-4541493e9060"));

            migrationBuilder.DeleteData(
                table: "AccessAuditEntries",
                keyColumn: "Id",
                keyValue: new Guid("25a03bf1-6107-d007-e1bc-24be1b53f3be"));

            migrationBuilder.DeleteData(
                table: "AccessAuditEntries",
                keyColumn: "Id",
                keyValue: new Guid("5cb9eac8-2fcd-a6f8-82a7-fb5125b6dc84"));

            migrationBuilder.DeleteData(
                table: "AccessAuditEntries",
                keyColumn: "Id",
                keyValue: new Guid("6f216ae0-209c-c092-4099-0043c21b3aa8"));

            migrationBuilder.DeleteData(
                table: "AccessGrants",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0012-0012-0012-000000000003"));

            migrationBuilder.DeleteData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("044990a0-297c-6041-7f72-96fd33ff3f5f"));

            migrationBuilder.DeleteData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("8dd50263-2800-0557-93b9-07a71d223c9b"));

            migrationBuilder.DeleteData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("b92706ba-8ccb-e7ef-266a-5ecab8db678a"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("0f35358b-a740-0f03-384d-771138ac802d"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("293debcc-96de-bc6a-7028-c71f703ef980"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("3e4a98bd-4067-59ac-66e0-6e5cb4137108"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("a32f168f-b981-c833-76f9-e98191ff9d3f"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("b0e7ddaa-2c3c-3d6f-e8fc-a47e3dc3fe71"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("e5e93f5e-8734-6c2f-751d-09b56c091735"));

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: new Guid("46a9dd3c-e522-7448-02ab-7696e37612eb"));

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: new Guid("be0c6821-74a8-fb28-c072-2e6dd368194f"));

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: new Guid("d5e36d15-3543-4d73-b0fc-af18dbec0030"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4d5a28c9-edc7-c3ae-e185-e86340b1787a"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("fb1e2d86-80da-a74a-d175-d17668f16149"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("243b1584-1695-7ee6-9356-12ca13425aaf"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("8d7f6b98-d232-0652-9708-e433e7f80ba1"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("ce8e714c-3cbb-8ef0-770c-dd6d3a6377c8"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("fd71bfe7-2fba-41ab-5548-ccdf4539f4df"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("3cc0937d-04de-51c0-4c38-f3a240ebcd8a"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("6e26ae2d-56bd-69c9-28fa-cc0c9c248a42"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("19a63320-87df-605a-721c-5740ad587879"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("83a1a2ee-e56f-cd4e-d5f5-47e46234fa75"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("91d410af-6706-1491-a2c4-b06a18c8ec72"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000090"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000091"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000090"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000091"));

            migrationBuilder.DropColumn(
                name: "ActionResolvedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActionTargetId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "BlNumber",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EmailSent",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EntityReference",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "Notifications");
        }
    }
}
