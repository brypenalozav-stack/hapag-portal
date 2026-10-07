using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShipmentDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BookingNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ContainerNumbers = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedForOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OnBehalfOfOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessGrantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentDetailId = table.Column<Guid>(type: "uuid", nullable: true),
                    GenerationKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VerificationCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SignatureId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SignatureProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SignatureLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TemplateJson = table.Column<string>(type: "text", nullable: false),
                    RecipientEmails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TermsVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TermsAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetainUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDocuments_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentDocumentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OnBehalfOfOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Recipient = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDocumentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDocumentEvents_ShipmentDocuments_ShipmentDocumentId",
                        column: x => x.ShipmentDocumentId,
                        principalTable: "ShipmentDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000012"), "HLCUSAI260501240", null, "HLCUBKG2605124", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "CL", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 10, 2, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 28, 0, 0, 0, 0, DateTimeKind.Utc), 4800m, "USD", new DateTime(2026, 10, 3, 15, 0, 0, 0, DateTimeKind.Utc), "Collect", null, false, false, null, null, "Agencia Marítima del Pacífico Ltda", null, null, "Santiago, Chile", "San Antonio (CLSAI)", "Yokohama (JPYOK)", "Import", "Yokohama Machinery Co.", "Arrived", "Cartagena Express", null, "2611E" },
                    { new Guid("11111111-0007-0007-0007-000000000013"), "HLCUVAP260501350", null, "HLCUBKG2605135", new Guid("c3d4e5f6-0003-0003-0003-000000000070"), "Global Forwarding Chile SpA", "CL", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 30, 0, 0, 0, 0, DateTimeKind.Utc), 2900m, "USD", null, null, null, false, false, null, null, null, null, null, "Valparaiso, Chile", "Valparaiso (CLVAP)", "Shanghai (CNSHA)", "Import", "Shanghai Furniture Ltd", "Arrived", "Callao Express", null, "2611N" }
                });

            migrationBuilder.InsertData(
                table: "ChargeConcepts",
                columns: new[] { "Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff" },
                values: new object[] { new Guid("3f585118-c4e9-dfe7-55a8-9ccdf3ad7b01"), "Service", "TRANSSHIPMENT_CERT", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 150, true, null, null, "Certificado de transbordo", false, false });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000024"), 35000m, new Guid("11111111-0007-0007-0007-000000000006"), "TRANSSHIPMENT_CERT", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Certificado de transbordo", true, null, null, "Paid", 6650m, 19m, 41650m },
                    { new Guid("33333333-0009-0009-0009-000000000025"), 150m, new Guid("11111111-0007-0007-0007-000000000005"), "ADVANCE_DEMURRAGE_BO", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Demoras anticipadas (1 contenedor(es))", false, null, null, "Paid", 0m, 0m, 150m }
                });

            migrationBuilder.InsertData(
                table: "MaintainerChangeLogs",
                columns: new[] { "Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue" },
                values: new object[] { new Guid("65387c59-9c85-0b9a-cbfb-175c8bde0570"), "Created", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("eeeeeeee-0014-0014-0014-000000000009"), "Tariff", "{\"conceptCode\":\"TRANSSHIPMENT_CERT\",\"code\":null,\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":null,\"description\":\"Certificado de transbordo (M6-01)\",\"amount\":35000,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null });

            migrationBuilder.InsertData(
                table: "ShipmentDocuments",
                columns: new[] { "Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0018-0018-0018-000000000001"), null, new Guid("11111111-0007-0007-0007-000000000006"), "HLCUSAI260300610", "HLCUBKG2603061", "HLXU2023001", null, "application/pdf", "CL", new DateTime(2026, 10, 4, 13, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 10, 4, 13, 0, 0, 0, DateTimeKind.Utc), "CTB-20261004-5A1B2C3D", "TransshipmentCertificate", "certificado-transbordo-CTB-20261004-5A1B2C3D.pdf", null, new DateTime(2026, 10, 4, 13, 0, 0, 0, DateTimeKind.Utc), null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, null, "Seed", null, null, "demo@importadorademo.cl", new DateTime(2036, 10, 4, 13, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, 0L, "Issued", null, "{\"title\":\"Certificado de transbordo\",\"subtitle\":\"Operaci\\u00F3n de exportaci\\u00F3n\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CTB-20261004-5A1B2C3D\",\"issuedAt\":\"2026-10-04T13:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260300610\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2603061\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Exportaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Valparaiso Express / 2610S\"},{\"label\":\"Ruta\",\"value\":\"San Antonio (CLSAI) - Rotterdam (NLRTM)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Consignee\",\"value\":\"Fruit Import BV\"},{\"label\":\"Notify\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Cliente\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"RUT / NIT\",\"value\":\"76123456-7\"},{\"label\":\"Pago\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU2023001\",\"40RF\",\"SL-020301\",\"26.800,00\",\"GateIn\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Certificaci\\u00F3n\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Hapag-Lloyd Chile SpA certifica que la carga amparada en el BL HLCUSAI260300610, transportada en la nave Valparaiso Express viaje 2610S, con origen en San Antonio (CLSAI) y destino Rotterdam, Netherlands, fue objeto de transbordo en el puerto de Rotterdam (NLRTM) en las unidades individualizadas en este documento.\",\"Se emite a solicitud del interesado para los fines que estime convenientes.\"]}],\"verificationCode\":\"EF67-5D1A-FCD1-C29B\",\"signatureNote\":\"Documento firmado electr\\u00F3nicamente. La validez de la firma se acredita seg\\u00FAn el mecanismo publicado por Hapag-Lloyd.\",\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}", null, null, null, "EF67-5D1A-FCD1-C29B" },
                    { new Guid("ffffffff-0018-0018-0018-000000000002"), null, new Guid("11111111-0007-0007-0007-000000000001"), "HLCUVAL250100123", "HLCUBKG2501001", "HLXU1234567,HLXU7654321", null, "application/pdf", "CL", new DateTime(2026, 10, 3, 16, 30, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", null, null, new DateTime(2026, 10, 3, 16, 30, 0, 0, DateTimeKind.Utc), "CBL-20261003-7E8F9A0B", "BlCopyNonValued", "copia-bl-no-valorada-CBL-20261003-7E8F9A0B.pdf", null, new DateTime(2026, 10, 3, 16, 30, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, null, "Seed", null, null, "demo@importadorademo.cl", new DateTime(2036, 10, 3, 16, 30, 0, 0, DateTimeKind.Utc), null, null, null, null, 0L, "Issued", null, "{\"title\":\"Copia de BL - no valorada\",\"subtitle\":\"Copia informativa, no negociable\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CBL-20261003-7E8F9A0B\",\"issuedAt\":\"2026-10-03T16:30:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUVAL250100123\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2501001\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Hamburg Express / 025E\"},{\"label\":\"Ruta\",\"value\":\"Shanghai (CNSHA) - San Antonio (CLSAI)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Shanghai Electronics Co. Ltd\"},{\"label\":\"Consignee\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Notify\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Transporte\",\"fields\":[{\"label\":\"Nave / viaje\",\"value\":\"Hamburg Express / 025E\"},{\"label\":\"Puerto de carga\",\"value\":\"Shanghai (CNSHA)\"},{\"label\":\"Puerto de descarga\",\"value\":\"San Antonio (CLSAI)\"},{\"label\":\"Lugar de entrega\",\"value\":\"Santiago, Chile\"},{\"label\":\"ETD\",\"value\":\"01-03-2026\"},{\"label\":\"ETA\",\"value\":\"05-04-2026\"},{\"label\":\"Tipo de BL\",\"value\":null},{\"label\":\"Incoterm\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU1234567\",\"40HC\",\"SL-001234\",\"24.500,00\",\"Discharged\"],[\"HLXU7654321\",\"20DV\",\"SL-005678\",\"18.200,00\",\"Discharged\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Mercanc\\u00EDa\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Sin detalle de mercanc\\u00EDa registrado.\"]},{\"heading\":\"Valores comerciales\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Copia no valorada: no incluye el flete ni los cargos del embarque.\"]},{\"heading\":\"Solicitud\",\"fields\":[{\"label\":\"Solicitada por\",\"value\":\"Importadora Demo SpA (demo@importadorademo.cl)\"}],\"table\":null,\"paragraphs\":null}],\"verificationCode\":\"90FF-9F4C-557C-3C9D\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}", null, null, null, "90FF-9F4C-557C-3C9D" }
                });

            migrationBuilder.InsertData(
                table: "Tariffs",
                columns: new[] { "Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo" },
                values: new object[] { new Guid("eeeeeeee-0014-0014-0014-000000000009"), 35000m, null, "TRANSSHIPMENT_CERT", null, "CL", new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Certificado de transbordo (M6-01)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000015"), new Guid("11111111-0007-0007-0007-000000000012"), "HLXU3045001", "40HC", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-045001", "Discharged", null, null, 25100m },
                    { new Guid("22222222-0008-0008-0008-000000000016"), new Guid("11111111-0007-0007-0007-000000000013"), "HLXU3045002", "20DV", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-045002", "Discharged", null, null, 17900m }
                });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[] { new Guid("33333333-0009-0009-0009-000000000023"), 185000m, new Guid("11111111-0007-0007-0007-000000000013"), "THC", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 20DV (Valparaíso)", true, null, null, "Pending", 35150m, 19m, 220150m });

            migrationBuilder.InsertData(
                table: "ShipmentDocumentEvents",
                columns: new[] { "Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId" },
                values: new object[,]
                {
                    { new Guid("81704c0f-1990-fcac-1ea8-2a34ced3fb44"), "Portal", null, "Issued", new DateTime(2026, 10, 3, 16, 30, 0, 0, DateTimeKind.Utc), null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, new Guid("ffffffff-0018-0018-0018-000000000002"), "demo@importadorademo.cl", null },
                    { new Guid("93928381-c81e-f01c-77a0-ab21529781c6"), "System", null, "Issued", new DateTime(2026, 10, 4, 13, 0, 0, 0, DateTimeKind.Utc), null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, new Guid("ffffffff-0018-0018-0018-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "ShipmentDocuments",
                columns: new[] { "Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0018-0018-0018-000000000003"), null, new Guid("11111111-0007-0007-0007-000000000012"), "HLCUSAI260501240", "HLCUBKG2605124", "HLXU3045001", null, "application/pdf", "CL", new DateTime(2026, 10, 3, 15, 5, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "CCO-20261003-1C2D3E4F", "CollectReceipt", "comprobante-collect-CCO-20261003-1C2D3E4F.pdf", null, new DateTime(2026, 10, 3, 15, 5, 0, 0, DateTimeKind.Utc), null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000030"), null, null, null, "Seed", null, null, null, new DateTime(2036, 10, 3, 15, 5, 0, 0, DateTimeKind.Utc), null, null, null, null, 0L, "Issued", null, "{\"title\":\"Comprobante de flete Collect\",\"subtitle\":null,\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CCO-20261003-1C2D3E4F\",\"issuedAt\":\"2026-10-03T15:05:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260501240\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2605124\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Cartagena Express / 2611E\"},{\"label\":\"Ruta\",\"value\":\"Yokohama (JPYOK) - San Antonio (CLSAI)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Yokohama Machinery Co.\"},{\"label\":\"Consignee\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Notify\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Pago del flete\",\"fields\":[{\"label\":\"Condici\\u00F3n del flete\",\"value\":\"Collect\"},{\"label\":\"Monto pagado\",\"value\":\"4.800,00 USD\"},{\"label\":\"Pagador\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"},{\"label\":\"RUT / NIT del pagador\",\"value\":\"96555444-3\"},{\"label\":\"Pago\",\"value\":null},{\"label\":\"Comprobante de pago\",\"value\":null},{\"label\":\"Fecha de pago\",\"value\":\"03-10-2026 12:00 (America/Santiago)\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3045001\",\"40HC\",\"SL-045001\",\"25.100,00\",\"Discharged\"]],\"numericColumns\":[3]},\"paragraphs\":null}],\"verificationCode\":\"B3D6-27E7-7995-FF24\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}", null, null, null, "B3D6-27E7-7995-FF24" },
                    { new Guid("ffffffff-0018-0018-0018-000000000004"), null, new Guid("11111111-0007-0007-0007-000000000013"), "HLCUVAP260501350", "HLCUBKG2605135", "HLXU3045002", null, "application/pdf", "CL", new DateTime(2026, 10, 2, 14, 0, 0, 0, DateTimeKind.Utc), "ffww@globalforwarding.cl", null, null, null, "CRE-20261002-9F8E7D6C", "ResponsibilityLetter", "carta-responsabilidad-CRE-20261002-9F8E7D6C.pdf", null, new DateTime(2026, 10, 2, 14, 0, 0, 0, DateTimeKind.Utc), "ffww@globalforwarding.cl", null, new Guid("c3d4e5f6-0003-0003-0003-000000000070"), null, null, null, "Seed", null, null, null, new DateTime(2036, 10, 2, 14, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, 0L, "Issued", null, "{\"title\":\"Carta de responsabilidad\",\"subtitle\":\"T\\u00E9rminos CARTA-RESP-2026-10\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CRE-20261002-9F8E7D6C\",\"issuedAt\":\"2026-10-02T14:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUVAP260501350\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2605135\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Callao Express / 2611N\"},{\"label\":\"Ruta\",\"value\":\"Shanghai (CNSHA) - Valparaiso (CLVAP)\"}],\"sections\":[{\"heading\":\"Organizaci\\u00F3n responsable\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Global Forwarding Chile SpA\"},{\"label\":\"RUT / NIT\",\"value\":\"76000003-3\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Firmante\",\"fields\":[{\"label\":\"Nombre\",\"value\":\"Felipe Forwarder\"},{\"label\":\"Documento de identidad\",\"value\":\"12.345.678-5\"},{\"label\":\"Cargo\",\"value\":\"Gerente de Operaciones\"},{\"label\":\"Correo de contacto\",\"value\":\"ffww@globalforwarding.cl\"},{\"label\":\"Tel\\u00E9fono\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3045002\",\"20DV\",\"SL-045002\",\"17.900,00\",\"Discharged\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Declaraci\\u00F3n\",\"fields\":[{\"label\":\"Mercanc\\u00EDa\",\"value\":\"Muebles de madera\"},{\"label\":\"Observaciones\",\"value\":null}],\"table\":null,\"paragraphs\":[\"El firmante, en representaci\\u00F3n de la organizaci\\u00F3n indicada, declara que los datos ingresados son ver\\u00EDdicos y asume ante Hapag-Lloyd la responsabilidad por la carga amparada en el BL individualizado, incluidos los cargos, demoras y perjuicios que se originen por su retiro y manipulaci\\u00F3n, liberando a Hapag-Lloyd de toda responsabilidad frente al consignatario final y a terceros.\",\"T\\u00E9rminos aceptados en el portal el 02-10-2026 11:00 (America/Santiago) (versi\\u00F3n CARTA-RESP-2026-10).\"]}],\"verificationCode\":\"94E7-43DA-1F4E-8AA7\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}", new DateTime(2026, 10, 2, 14, 0, 0, 0, DateTimeKind.Utc), "CARTA-RESP-2026-10", null, "94E7-43DA-1F4E-8AA7" }
                });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[,]
                {
                    { new Guid("464a1f72-cc7e-cb5e-aab3-e55e60f9a7bd"), new Guid("11111111-0007-0007-0007-000000000012"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" },
                    { new Guid("6195e2af-bf9f-a6ca-ff6f-e3311b80ac45"), new Guid("11111111-0007-0007-0007-000000000012"), new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "CustomsAgency", "Seed" },
                    { new Guid("e3596ff0-7c41-8ca2-284a-837b029aed7f"), new Guid("11111111-0007-0007-0007-000000000013"), new Guid("c3d4e5f6-0003-0003-0003-000000000070"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Consignee", "Seed" }
                });

            migrationBuilder.InsertData(
                table: "ShipmentDocumentEvents",
                columns: new[] { "Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId" },
                values: new object[,]
                {
                    { new Guid("853b0d16-b00e-352a-fa93-99c6d48f2438"), "Portal", null, "Issued", new DateTime(2026, 10, 2, 14, 0, 0, 0, DateTimeKind.Utc), null, new Guid("c3d4e5f6-0003-0003-0003-000000000070"), null, new Guid("ffffffff-0018-0018-0018-000000000004"), "ffww@globalforwarding.cl", null },
                    { new Guid("c63bfeee-1240-a9e9-1e0a-56b43357013b"), "System", null, "Issued", new DateTime(2026, 10, 3, 15, 5, 0, 0, DateTimeKind.Utc), null, new Guid("c3d4e5f6-0003-0003-0003-000000000030"), null, new Guid("ffffffff-0018-0018-0018-000000000003"), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentEvents_OrganizationId_OccurredAt",
                table: "ShipmentDocumentEvents",
                columns: new[] { "OrganizationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentEvents_ShipmentDocumentId_OccurredAt",
                table: "ShipmentDocumentEvents",
                columns: new[] { "ShipmentDocumentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_BillOfLadingId_DocumentType_IssuedAt",
                table: "ShipmentDocuments",
                columns: new[] { "BillOfLadingId", "DocumentType", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_DocumentNumber",
                table: "ShipmentDocuments",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_GenerationKey",
                table: "ShipmentDocuments",
                column: "GenerationKey",
                unique: true,
                filter: "\"GenerationKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_IssuedForOrganizationId_DocumentType_Stat~",
                table: "ShipmentDocuments",
                columns: new[] { "IssuedForOrganizationId", "DocumentType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_PaymentId",
                table: "ShipmentDocuments",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentDocumentEvents");

            migrationBuilder.DropTable(
                name: "ShipmentDocuments");

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000015"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000016"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("3f585118-c4e9-dfe7-55a8-9ccdf3ad7b01"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000023"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000024"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000025"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("65387c59-9c85-0b9a-cbfb-175c8bde0570"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("464a1f72-cc7e-cb5e-aab3-e55e60f9a7bd"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("6195e2af-bf9f-a6ca-ff6f-e3311b80ac45"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("e3596ff0-7c41-8ca2-284a-837b029aed7f"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0014-0014-0014-000000000009"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000012"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000013"));
        }
    }
}
