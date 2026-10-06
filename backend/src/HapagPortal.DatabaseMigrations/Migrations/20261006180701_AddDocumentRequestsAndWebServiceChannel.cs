using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentRequestsAndWebServiceChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiClients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Scopes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false),
                    SignatoryName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SignatoryTaxId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SignatoryPosition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SignatoryEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TechnicalContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiClients_Clients_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApiClients_Users_TechnicalUserId",
                        column: x => x.TechnicalUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssistantDocumentDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OnBehalfOfOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentKind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantDocumentDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantDocumentDeliveries_AssistantSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AssistantSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseLetterRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalEntityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CarrierOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TatcAvailableAtSubmission = table.Column<bool>(type: "boolean", nullable: false),
                    TatcStatusAtSubmission = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TatcErrorAtSubmission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TatcCheckedAtSubmission = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TatcSnapshotAtSubmission = table.Column<string>(type: "text", nullable: true),
                    TatcAvailableAtApproval = table.Column<bool>(type: "boolean", nullable: true),
                    TatcStatusAtApproval = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TatcErrorAtApproval = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TatcCheckedAtApproval = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TatcSnapshotAtApproval = table.Column<string>(type: "text", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseLetterRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseLetterRequests_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiClientKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClientKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiClientKeys_ApiClients_ApiClientId",
                        column: x => x.ApiClientId,
                        principalTable: "ApiClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApiClientRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiClientKeyId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    TargetType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClientRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiClientRequests_ApiClients_ApiClientId",
                        column: x => x.ApiClientId,
                        principalTable: "ApiClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ChargeConcepts",
                columns: new[] { "Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff" },
                values: new object[,]
                {
                    { new Guid("7bd6dee1-8b2d-df8c-222b-3ccd1227bf8c"), "Service", "FREIGHT_CERTIFICATE", "BO", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 330, true, null, null, "Certificado de flete", false, false },
                    { new Guid("b8406cc8-7a30-756b-b1d6-9509cfe805f4"), "Service", "RELEASE_LETTER", "BO", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 340, true, null, null, "Carta de liberación y desconsolidado", false, false }
                });

            migrationBuilder.InsertData(
                table: "MaintainerChangeLogs",
                columns: new[] { "Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue" },
                values: new object[,]
                {
                    { new Guid("5b6d64e3-30e7-2094-9668-48c5b0f4624f"), "Created", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0022-0022-0022-000000000003"), "ServiceDefinition", "{\"code\":\"RELEASE_LETTER\",\"nameEs\":\"Carta de liberaci\\u00F3n y desconsolidado\",\"nameEn\":\"Release and deconsolidation letter\",\"descriptionEs\":\"Carta de liberaci\\u00F3n y desconsolidado de las unidades seleccionadas, importaci\\u00F3n de Bolivia (M6-08, BO-IMP-11): datos del consignatario seg\\u00FAn el tipo de sociedad y del transportista; el TATC de las unidades se registra al enviar y al aprobar; la aprueba Customer Service y la carta se emite al aprobarse. Sin cobro. Se solicita desde los documentos del embarque.\",\"descriptionEn\":\"Release and deconsolidation letter for the selected units, Bolivia imports (M6-08): consignee data by legal entity type and carrier data; the units\\u0027 TATC is recorded on submission and approval; Customer Service approves it and the letter is issued on approval. No charge. Requested from the shipment documents.\",\"operations\":\"IMPORT\",\"countries\":\"BO\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":true,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022containers\\u0022,\\u0022labelEs\\u0022:\\u0022Contenedores\\u0022,\\u0022labelEn\\u0022:\\u0022Containers\\u0022,\\u0022type\\u0022:\\u0022containers\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022legalEntityType\\u0022,\\u0022labelEs\\u0022:\\u0022Tipo de sociedad\\u0022,\\u0022labelEn\\u0022:\\u0022Legal entity type\\u0022,\\u0022type\\u0022:\\u0022select\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:[{\\u0022value\\u0022:\\u0022COMPANY\\u0022,\\u0022labelEs\\u0022:\\u0022Empresa\\u0022,\\u0022labelEn\\u0022:\\u0022Company\\u0022},{\\u0022value\\u0022:\\u0022NATURAL_PERSON\\u0022,\\u0022labelEs\\u0022:\\u0022Persona natural\\u0022,\\u0022labelEn\\u0022:\\u0022Natural person\\u0022}],\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022consigneeName\\u0022,\\u0022labelEs\\u0022:\\u0022Consignatario (raz\\\\u00F3n social o nombre)\\u0022,\\u0022labelEn\\u0022:\\u0022Consignee (legal name or name)\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:200,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022consigneeTaxId\\u0022,\\u0022labelEs\\u0022:\\u0022NIT o documento de identidad\\u0022,\\u0022labelEn\\u0022:\\u0022Tax ID or ID document\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:30,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022consigneeAddress\\u0022,\\u0022labelEs\\u0022:\\u0022Domicilio\\u0022,\\u0022labelEn\\u0022:\\u0022Address\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:300,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022legalRepresentativeName\\u0022,\\u0022labelEs\\u0022:\\u0022Representante legal\\u0022,\\u0022labelEn\\u0022:\\u0022Legal representative\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:200,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022legalRepresentativeId\\u0022,\\u0022labelEs\\u0022:\\u0022Documento del representante\\u0022,\\u0022labelEn\\u0022:\\u0022Representative\\\\u0027s ID\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:30,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022carrierName\\u0022,\\u0022labelEs\\u0022:\\u0022Transportista\\u0022,\\u0022labelEn\\u0022:\\u0022Carrier\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:200,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022carrierTaxId\\u0022,\\u0022labelEs\\u0022:\\u0022NIT / RUT del transportista\\u0022,\\u0022labelEn\\u0022:\\u0022Carrier tax ID\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:30,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022driverName\\u0022,\\u0022labelEs\\u0022:\\u0022Conductor\\u0022,\\u0022labelEn\\u0022:\\u0022Driver\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:200,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022driverId\\u0022,\\u0022labelEs\\u0022:\\u0022Documento del conductor\\u0022,\\u0022labelEn\\u0022:\\u0022Driver\\\\u0027s ID\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:30,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022truckPlate\\u0022,\\u0022labelEs\\u0022:\\u0022Patente\\u0022,\\u0022labelEn\\u0022:\\u0022Truck plate\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:20,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022observations\\u0022,\\u0022labelEs\\u0022:\\u0022Observaciones\\u0022,\\u0022labelEn\\u0022:\\u0022Remarks\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":false,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"None\",\"chargeConceptCode\":\"RELEASE_LETTER\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":false,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"CustomerService\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"release-letter.generate\",\"displayOrder\":140,\"isActive\":true}", null },
                    { new Guid("a586d1a7-d19d-2034-2f92-6cb6c8f53a5c"), "Created", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ffffffff-0022-0022-0022-000000000002"), "ServiceDefinition", "{\"code\":\"FREIGHT_CERTIFICATE\",\"nameEs\":\"Certificado de flete\",\"nameEn\":\"Freight certificate\",\"descriptionEs\":\"Certificado del flete del BL para importaci\\u00F3n de Bolivia (M6-02, BO-IMP-08): el cliente ingresa los datos del consignatario y la finalidad; el portal emite el PDF firmado y lo env\\u00EDa a su correo. Primera entrega de Fase 2 sin pago ni carro. Se solicita desde los documentos del embarque.\",\"descriptionEn\":\"Freight certificate of the BL for Bolivia imports (M6-02): the customer enters the consignee data and the purpose; the portal issues the signed PDF and e-mails it. First Phase 2 delivery without payment or cart. Requested from the shipment documents.\",\"operations\":\"IMPORT\",\"countries\":\"BO\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":false,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022consigneeName\\u0022,\\u0022labelEs\\u0022:\\u0022Consignatario\\u0022,\\u0022labelEn\\u0022:\\u0022Consignee\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:200,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022consigneeTaxId\\u0022,\\u0022labelEs\\u0022:\\u0022NIT del consignatario\\u0022,\\u0022labelEn\\u0022:\\u0022Consignee tax ID\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:30,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022purpose\\u0022,\\u0022labelEs\\u0022:\\u0022Finalidad\\u0022,\\u0022labelEn\\u0022:\\u0022Purpose\\u0022,\\u0022type\\u0022:\\u0022select\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:[{\\u0022value\\u0022:\\u0022CUSTOMS\\u0022,\\u0022labelEs\\u0022:\\u0022Tr\\\\u00E1mite aduanero\\u0022,\\u0022labelEn\\u0022:\\u0022Customs clearance\\u0022},{\\u0022value\\u0022:\\u0022INSURANCE\\u0022,\\u0022labelEs\\u0022:\\u0022Seguro de la carga\\u0022,\\u0022labelEn\\u0022:\\u0022Cargo insurance\\u0022},{\\u0022value\\u0022:\\u0022BANK\\u0022,\\u0022labelEs\\u0022:\\u0022Tr\\\\u00E1mite bancario\\u0022,\\u0022labelEn\\u0022:\\u0022Banking\\u0022},{\\u0022value\\u0022:\\u0022OTHER\\u0022,\\u0022labelEs\\u0022:\\u0022Otra\\u0022,\\u0022labelEn\\u0022:\\u0022Other\\u0022}],\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022recipient\\u0022,\\u0022labelEs\\u0022:\\u0022Dirigido a\\u0022,\\u0022labelEn\\u0022:\\u0022Addressed to\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:200,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022notes\\u0022,\\u0022labelEs\\u0022:\\u0022Observaciones\\u0022,\\u0022labelEn\\u0022:\\u0022Remarks\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":false,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"None\",\"chargeConceptCode\":\"FREIGHT_CERTIFICATE\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":false,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"freight-certificate.generate\",\"displayOrder\":130,\"isActive\":true}", null }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[] { new Guid("4e1a9a66-9f8f-e94f-f54e-45976fd5f9ec"), "api-clients.manage", "api-clients.manage" });

            migrationBuilder.InsertData(
                table: "ServiceDefinitions",
                columns: new[] { "Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0022-0022-0022-000000000002"), "freight-certificate.generate", true, "None", "Always", false, "FREIGHT_CERTIFICATE", "FREIGHT_CERTIFICATE", "BO", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Freight certificate of the BL for Bolivia imports (M6-02): the customer enters the consignee data and the purpose; the portal issues the signed PDF and e-mails it. First Phase 2 delivery without payment or cart. Requested from the shipment documents.", "Certificado del flete del BL para importación de Bolivia (M6-02, BO-IMP-08): el cliente ingresa los datos del consignatario y la finalidad; el portal emite el PDF firmado y lo envía a su correo. Primera entrega de Fase 2 sin pago ni carro. Se solicita desde los documentos del embarque.", 130, false, null, "None", "[{\"key\":\"consigneeName\",\"labelEs\":\"Consignatario\",\"labelEn\":\"Consignee\",\"type\":\"text\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":200,\"helpEs\":null,\"helpEn\":null},{\"key\":\"consigneeTaxId\",\"labelEs\":\"NIT del consignatario\",\"labelEn\":\"Consignee tax ID\",\"type\":\"text\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":30,\"helpEs\":null,\"helpEn\":null},{\"key\":\"purpose\",\"labelEs\":\"Finalidad\",\"labelEn\":\"Purpose\",\"type\":\"select\",\"required\":true,\"options\":[{\"value\":\"CUSTOMS\",\"labelEs\":\"Tr\\u00E1mite aduanero\",\"labelEn\":\"Customs clearance\"},{\"value\":\"INSURANCE\",\"labelEs\":\"Seguro de la carga\",\"labelEn\":\"Cargo insurance\"},{\"value\":\"BANK\",\"labelEs\":\"Tr\\u00E1mite bancario\",\"labelEn\":\"Banking\"},{\"value\":\"OTHER\",\"labelEs\":\"Otra\",\"labelEn\":\"Other\"}],\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"recipient\",\"labelEs\":\"Dirigido a\",\"labelEn\":\"Addressed to\",\"type\":\"text\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":200,\"helpEs\":null,\"helpEn\":null},{\"key\":\"notes\",\"labelEs\":\"Observaciones\",\"labelEn\":\"Remarks\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "None", 0, null, null, "Freight certificate", "Certificado de flete", "IMPORT", "None", "PerRequest", "BL", null, false, false, false, null, false, "None" },
                    { new Guid("ffffffff-0022-0022-0022-000000000003"), "release-letter.generate", true, "CustomerService", "Always", false, "RELEASE_LETTER", "RELEASE_LETTER", "BO", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Release and deconsolidation letter for the selected units, Bolivia imports (M6-08): consignee data by legal entity type and carrier data; the units' TATC is recorded on submission and approval; Customer Service approves it and the letter is issued on approval. No charge. Requested from the shipment documents.", "Carta de liberación y desconsolidado de las unidades seleccionadas, importación de Bolivia (M6-08, BO-IMP-11): datos del consignatario según el tipo de sociedad y del transportista; el TATC de las unidades se registra al enviar y al aprobar; la aprueba Customer Service y la carta se emite al aprobarse. Sin cobro. Se solicita desde los documentos del embarque.", 140, false, null, "None", "[{\"key\":\"containers\",\"labelEs\":\"Contenedores\",\"labelEn\":\"Containers\",\"type\":\"containers\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"legalEntityType\",\"labelEs\":\"Tipo de sociedad\",\"labelEn\":\"Legal entity type\",\"type\":\"select\",\"required\":true,\"options\":[{\"value\":\"COMPANY\",\"labelEs\":\"Empresa\",\"labelEn\":\"Company\"},{\"value\":\"NATURAL_PERSON\",\"labelEs\":\"Persona natural\",\"labelEn\":\"Natural person\"}],\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"consigneeName\",\"labelEs\":\"Consignatario (raz\\u00F3n social o nombre)\",\"labelEn\":\"Consignee (legal name or name)\",\"type\":\"text\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":200,\"helpEs\":null,\"helpEn\":null},{\"key\":\"consigneeTaxId\",\"labelEs\":\"NIT o documento de identidad\",\"labelEn\":\"Tax ID or ID document\",\"type\":\"text\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":30,\"helpEs\":null,\"helpEn\":null},{\"key\":\"consigneeAddress\",\"labelEs\":\"Domicilio\",\"labelEn\":\"Address\",\"type\":\"text\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":300,\"helpEs\":null,\"helpEn\":null},{\"key\":\"legalRepresentativeName\",\"labelEs\":\"Representante legal\",\"labelEn\":\"Legal representative\",\"type\":\"text\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":200,\"helpEs\":null,\"helpEn\":null},{\"key\":\"legalRepresentativeId\",\"labelEs\":\"Documento del representante\",\"labelEn\":\"Representative\\u0027s ID\",\"type\":\"text\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":30,\"helpEs\":null,\"helpEn\":null},{\"key\":\"carrierName\",\"labelEs\":\"Transportista\",\"labelEn\":\"Carrier\",\"type\":\"text\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":200,\"helpEs\":null,\"helpEn\":null},{\"key\":\"carrierTaxId\",\"labelEs\":\"NIT / RUT del transportista\",\"labelEn\":\"Carrier tax ID\",\"type\":\"text\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":30,\"helpEs\":null,\"helpEn\":null},{\"key\":\"driverName\",\"labelEs\":\"Conductor\",\"labelEn\":\"Driver\",\"type\":\"text\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":200,\"helpEs\":null,\"helpEn\":null},{\"key\":\"driverId\",\"labelEs\":\"Documento del conductor\",\"labelEn\":\"Driver\\u0027s ID\",\"type\":\"text\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":30,\"helpEs\":null,\"helpEn\":null},{\"key\":\"truckPlate\",\"labelEs\":\"Patente\",\"labelEn\":\"Truck plate\",\"type\":\"text\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":20,\"helpEs\":null,\"helpEn\":null},{\"key\":\"observations\",\"labelEs\":\"Observaciones\",\"labelEn\":\"Remarks\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "None", 0, null, null, "Release and deconsolidation letter", "Carta de liberación y desconsolidado", "IMPORT", "None", "PerRequest", "BL", null, true, false, false, null, false, "None" }
                });

            migrationBuilder.InsertData(
                table: "ShipmentDocuments",
                columns: new[] { "Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode" },
                values: new object[] { new Guid("ffffffff-0018-0018-0018-000000000006"), null, new Guid("11111111-0007-0007-0007-000000000005"), "HLCUIQQ260200078", "HLCUBKG2602078", "HLXU5566778,HLXU5566779", null, "application/pdf", "BO", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", null, null, new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), "CFL-20261005-3B4C5D6E", "FreightCertificate", "certificado-flete-CFL-20261005-3B4C5D6E.pdf", "service-request:ffffffff-0021-0021-0021-000000000010", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, null, "Seed", null, null, "demo@altiplano.bo", new DateTime(2036, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, 0L, "Issued", null, "{\"title\":\"Certificado de flete\",\"subtitle\":\"Importaci\\u00F3n - Bolivia\",\"issuer\":\"Hapag-Lloyd Bolivia S.R.L.\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Bolivia\",\"documentNumber\":\"CFL-20261005-3B4C5D6E\",\"issuedAt\":\"2026-10-05T14:00:00Z\",\"timeZoneId\":\"America/La_Paz\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUIQQ260200078\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2602078\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"BO\"},{\"label\":\"Nave / viaje\",\"value\":\"Guayaquil Express / 007W\"},{\"label\":\"Ruta\",\"value\":\"Mumbai (INBOM) - Iquique (CLIQQ)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Mumbai Spices \\u0026 Commodities Pvt Ltd\"},{\"label\":\"Consignee\",\"value\":\"Comercial Altiplano SRL\"},{\"label\":\"Notify\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Solicitud\",\"fields\":[{\"label\":\"Solicitud\",\"value\":\"SRV-20261005-5E1A0010\"},{\"label\":\"Solicitada por\",\"value\":\"Comercial Altiplano SRL (1023456017)\"},{\"label\":\"Consignatario\",\"value\":\"Comercial Altiplano SRL (1023456017)\"},{\"label\":\"Finalidad\",\"value\":\"Tr\\u00E1mite aduanero\"},{\"label\":\"Dirigido a\",\"value\":\"Aduana Nacional de Bolivia\"},{\"label\":\"Observaciones\",\"value\":\"Para la declaraci\\u00F3n de importaci\\u00F3n (DIM).\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Flete\",\"fields\":[{\"label\":\"Condici\\u00F3n del flete\",\"value\":null},{\"label\":\"Monto del flete\",\"value\":\"1.950,00 USD\"},{\"label\":\"Puerto de carga\",\"value\":\"Mumbai (INBOM)\"},{\"label\":\"Puerto de descarga\",\"value\":\"Iquique (CLIQQ)\"},{\"label\":\"Lugar de entrega\",\"value\":\"Santa Cruz, Bolivia\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU5566778\",\"40HC\",\"SL-015678\",\"21.300,00\",\"OnBoard\"],[\"HLXU5566779\",\"20DV\",\"SL-015679\",\"14.900,00\",\"Discharged\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Mercanc\\u00EDa\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Sin detalle de mercanc\\u00EDa registrado.\"]},{\"heading\":\"Certificaci\\u00F3n\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Hapag-Lloyd Bolivia S.R.L. certifica que el flete mar\\u00EDtimo de la carga amparada en el BL HLCUIQQ260200078, transportada en la nave Guayaquil Express viaje 007W desde Mumbai (INBOM) hasta Santa Cruz, Bolivia, asciende a 1.950,00 USD, seg\\u00FAn el registro del embarque a la fecha de emisi\\u00F3n.\",\"Se emite a solicitud del interesado para la finalidad declarada.\"]}],\"verificationCode\":\"7900-1AF6-3B9B-DCF4\",\"signatureNote\":\"Documento firmado electr\\u00F3nicamente. La validez de la firma se acredita seg\\u00FAn el mecanismo publicado por Hapag-Lloyd.\",\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}", null, null, null, "7900-1AF6-3B9B-DCF4" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username" },
                values: new object[] { new Guid("d4e5f6a7-0004-0004-0004-000000000092"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "CL", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "ws-ffffffff003300330033000000000001@clients.ws.invalid", null, null, "Web Service", true, true, null, "ERP Importadora Demo", new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", "Active", null, null, "$2a$12$O1N2kndfeGWg41xxfI/dcOwtqqctzfoR6cdyUT./IfSvcU4NaDUGe", null, null, null, null, null, "Technical", "ws-ffffffff003300330033000000000001@clients.ws.invalid" });

            migrationBuilder.InsertData(
                table: "ApiClients",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Name", "Notes", "OrganizationId", "RateLimitPerMinute", "RevocationReason", "RevokedAt", "RevokedBy", "Scopes", "SignatoryEmail", "SignatoryName", "SignatoryPosition", "SignatoryTaxId", "Status", "TechnicalContactEmail", "TechnicalUserId" },
                values: new object[] { new Guid("ffffffff-0033-0033-0033-000000000001"), new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", null, null, null, null, "ERP Importadora Demo", "Cliente de demostración del canal Web Service (Ola J).", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), 60, null, null, null, "responsibility-letter,warehouse-change", "demo@importadorademo.cl", "Daniela Demo", "Gerente de Comercio Exterior", "15.678.901-2", "Active", "ti@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000092") });

            migrationBuilder.InsertData(
                table: "ServiceRequests",
                columns: new[] { "Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0021-0021-0021-000000000010"), null, 0m, null, null, new Guid("11111111-0007-0007-0007-000000000005"), null, null, null, null, null, "HLCUIQQ260200078", "HLCUBKG2602078", null, null, new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), null, "BO", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", null, "FREIGHT_CERTIFICATE", new Guid("ffffffff-0022-0022-0022-000000000002"), null, null, null, "{\"consigneeName\":\"Comercial Altiplano SRL\",\"consigneeTaxId\":\"1023456017\",\"purpose\":\"CUSTOMS\",\"recipient\":\"Aduana Nacional de Bolivia\",\"notes\":\"Para la declaración de importación (DIM).\"}", false, null, null, null, null, null, null, "IMPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, null, 1, null, null, "SRV-20261005-5E1A0010", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), "Certificado CFL-20261005-3B4C5D6E emitido.", "Completed", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, 0m, null, 3, null, 0m },
                    { new Guid("ffffffff-0021-0021-0021-000000000011"), null, 0m, null, "CustomerService", new Guid("11111111-0007-0007-0007-000000000004"), null, null, null, null, null, "HLCUARI260100045", "HLCUBKG2601045", null, null, null, "HLXU8899001", "BO", new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", null, "RELEASE_LETTER", new Guid("ffffffff-0022-0022-0022-000000000003"), null, null, null, "{\"containers\":[\"HLXU8899001\"],\"legalEntityType\":\"COMPANY\",\"consigneeName\":\"Comercial Altiplano SRL\",\"consigneeTaxId\":\"1023456017\",\"consigneeAddress\":\"Av. Arce 2631, La Paz\",\"legalRepresentativeName\":\"Marcela Quispe\",\"legalRepresentativeId\":\"4876512 LP\",\"carrierName\":\"Transportes Illimani SRL\",\"carrierTaxId\":\"4455667018\",\"driverName\":\"Juan Mamani\",\"driverId\":\"6123987 LP\",\"truckPlate\":\"2345-KTR\"}", false, null, null, null, null, null, null, "IMPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, null, 1, null, null, "SRV-20261005-5E1A0011", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), null, "PendingApproval", new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, 0m, null, 3, null, 0m }
                });

            migrationBuilder.InsertData(
                table: "ShipmentDocumentEvents",
                columns: new[] { "Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId" },
                values: new object[] { new Guid("5f032085-9241-c2f6-7fd5-5f1258c1dc9c"), "Portal", null, "Issued", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), null, new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, new Guid("ffffffff-0018-0018-0018-000000000006"), "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020") });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "RoleId", "RoleName", "UserId" },
                values: new object[] { new Guid("8b85820f-23d8-1ed6-e46f-1001c7af915c"), new Guid("227e87c5-a8d9-51c6-e0e9-373dc6d2c60b"), "OrgOperator", new Guid("d4e5f6a7-0004-0004-0004-000000000092") });

            migrationBuilder.InsertData(
                table: "ApiClientKeys",
                columns: new[] { "Id", "ApiClientId", "CreatedAt", "CreatedBy", "ExpiresAt", "KeyHash", "LastUsedAt", "Prefix", "RevokedAt", "RevokedBy" },
                values: new object[] { new Guid("ffffffff-0033-0033-0033-000000000002"), new Guid("ffffffff-0033-0033-0033-000000000001"), new DateTime(2026, 10, 5, 12, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", null, "b23557bc92386afaa0dacb9c9c48adc6554223090784f268cbe3fba4bd93e6ba", null, "4pnrf9yxigh5", null, null });

            migrationBuilder.InsertData(
                table: "ReleaseLetterRequests",
                columns: new[] { "Id", "CarrierOrganizationId", "DocumentId", "LegalEntityType", "ServiceRequestId", "TatcAvailableAtApproval", "TatcAvailableAtSubmission", "TatcCheckedAtApproval", "TatcCheckedAtSubmission", "TatcErrorAtApproval", "TatcErrorAtSubmission", "TatcSnapshotAtApproval", "TatcSnapshotAtSubmission", "TatcStatusAtApproval", "TatcStatusAtSubmission" },
                values: new object[] { new Guid("ffffffff-0032-0032-0032-000000000001"), null, null, "COMPANY", new Guid("ffffffff-0021-0021-0021-000000000011"), null, true, null, new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), null, null, null, "[{\"containerNumber\":\"HLXU8899001\",\"status\":\"NotIssued\",\"sourceStatus\":\"NOT_ISSUED\",\"tatcNumber\":null,\"pendingReasons\":[\"PAYMENT_PENDING\",\"MHD_PENDING\"]}]", null, "NotIssued" });

            migrationBuilder.InsertData(
                table: "ServiceRequestEvents",
                columns: new[] { "Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus" },
                values: new object[,]
                {
                    { new Guid("0d5309ac-17ef-dae7-dbe7-375b1e62c38f"), "Client", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), null, null, new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000011"), "Draft" },
                    { new Guid("2adb63cd-684f-cca7-e35d-e9e73e299a1e"), "Client", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), "Draft", null, new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000011"), "Submitted" },
                    { new Guid("7fbd7dc7-3378-6e62-9ba9-0911de05fe7c"), "System", "SYSTEM", null, "Submitted", "Certificado CFL-20261005-3B4C5D6E emitido, sin cobro (primera entrega de Fase 2, M6-02).", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000010"), "Completed" },
                    { new Guid("ab6f4921-65c2-6608-23e0-d46e8d0ee864"), "Client", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), "Draft", null, new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000010"), "Submitted" },
                    { new Guid("af9b468e-db5e-890c-63af-2c8716f674d1"), "Client", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), null, null, new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000010"), "Draft" },
                    { new Guid("bb6e05ed-c743-6c57-d75e-db85983752ae"), "System", "SYSTEM", null, "Submitted", "Derivada al equipo CustomerService. Al enviar: TATC NotIssued: HLXU8899001 NotIssued.", new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000011"), "PendingApproval" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientKeys_ApiClientId",
                table: "ApiClientKeys",
                column: "ApiClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientKeys_Prefix",
                table: "ApiClientKeys",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientRequests_ApiClientId_IdempotencyKey",
                table: "ApiClientRequests",
                columns: new[] { "ApiClientId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientRequests_ApiClientId_ReceivedAt",
                table: "ApiClientRequests",
                columns: new[] { "ApiClientId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientRequests_OrganizationId_ReceivedAt",
                table: "ApiClientRequests",
                columns: new[] { "OrganizationId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_OrganizationId",
                table: "ApiClients",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_TechnicalUserId",
                table: "ApiClients",
                column: "TechnicalUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssistantDocumentDeliveries_DocumentId_DeliveredAt",
                table: "AssistantDocumentDeliveries",
                columns: new[] { "DocumentId", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantDocumentDeliveries_SessionId_DeliveredAt",
                table: "AssistantDocumentDeliveries",
                columns: new[] { "SessionId", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantDocumentDeliveries_UserId_DeliveredAt",
                table: "AssistantDocumentDeliveries",
                columns: new[] { "UserId", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseLetterRequests_ServiceRequestId",
                table: "ReleaseLetterRequests",
                column: "ServiceRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiClientKeys");

            migrationBuilder.DropTable(
                name: "ApiClientRequests");

            migrationBuilder.DropTable(
                name: "AssistantDocumentDeliveries");

            migrationBuilder.DropTable(
                name: "ReleaseLetterRequests");

            migrationBuilder.DropTable(
                name: "ApiClients");

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("7bd6dee1-8b2d-df8c-222b-3ccd1227bf8c"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("b8406cc8-7a30-756b-b1d6-9509cfe805f4"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("5b6d64e3-30e7-2094-9668-48c5b0f4624f"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("a586d1a7-d19d-2034-2f92-6cb6c8f53a5c"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4e1a9a66-9f8f-e94f-f54e-45976fd5f9ec"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("0d5309ac-17ef-dae7-dbe7-375b1e62c38f"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("2adb63cd-684f-cca7-e35d-e9e73e299a1e"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("7fbd7dc7-3378-6e62-9ba9-0911de05fe7c"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("ab6f4921-65c2-6608-23e0-d46e8d0ee864"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("af9b468e-db5e-890c-63af-2c8716f674d1"));

            migrationBuilder.DeleteData(
                table: "ServiceRequestEvents",
                keyColumn: "Id",
                keyValue: new Guid("bb6e05ed-c743-6c57-d75e-db85983752ae"));

            migrationBuilder.DeleteData(
                table: "ShipmentDocumentEvents",
                keyColumn: "Id",
                keyValue: new Guid("5f032085-9241-c2f6-7fd5-5f1258c1dc9c"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("8b85820f-23d8-1ed6-e46f-1001c7af915c"));

            migrationBuilder.DeleteData(
                table: "ServiceRequests",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0021-0021-0021-000000000010"));

            migrationBuilder.DeleteData(
                table: "ServiceRequests",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0021-0021-0021-000000000011"));

            migrationBuilder.DeleteData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000006"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000092"));

            migrationBuilder.DeleteData(
                table: "ServiceDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0022-0022-0022-000000000002"));

            migrationBuilder.DeleteData(
                table: "ServiceDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0022-0022-0022-000000000003"));
        }
    }
}
