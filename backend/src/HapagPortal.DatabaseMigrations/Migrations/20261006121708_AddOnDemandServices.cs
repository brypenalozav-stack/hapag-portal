using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOnDemandServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NameEs = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DescriptionEs = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Operations = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Countries = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequiredBlStatuses = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AvailabilityWindow = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequiresContainers = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMultiplePerBl = table.Column<bool>(type: "boolean", nullable: false),
                    InputSchemaJson = table.Column<string>(type: "text", nullable: false),
                    BillingDataRequired = table.Column<bool>(type: "boolean", nullable: false),
                    TariffAcceptanceRequired = table.Column<bool>(type: "boolean", nullable: false),
                    PricingMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChargeConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TariffCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LateTariffCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    QuantityMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MeasureFieldKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Milestone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MilestoneOffsetHours = table.Column<int>(type: "integer", nullable: false),
                    DeadlineRuleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TimingRule = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Taxable = table.Column<bool>(type: "boolean", nullable: false),
                    ExemptionConcept = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExcludeShipperOwnedContainers = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalTeam = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FulfillmentTeam = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequiresOutputDocument = table.Column<bool>(type: "boolean", nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_ServiceDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OnBehalfOfClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessGrantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BookingNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Operation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ContainerNumbers = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InputValuesJson = table.Column<string>(type: "text", nullable: false),
                    BillingTaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BillingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BillingEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BillingActivity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChargeConceptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TariffId = table.Column<Guid>(type: "uuid", nullable: true),
                    TariffCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TariffSource = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TierUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MeasuredUnits = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Timing = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MilestoneAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MilestoneSource = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    PricingDetailJson = table.Column<string>(type: "text", nullable: true),
                    QuotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TariffAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsExempt = table.Column<bool>(type: "boolean", nullable: false),
                    ExemptionReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedTeam = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TimelineSequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_ServiceDefinitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "ServiceDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequestAttachments_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequestCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Generated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequestCharges_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequestEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequestEvents_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[] { new Guid("22222222-0008-0008-0008-000000000022"), new Guid("11111111-0007-0007-0007-000000000005"), "HLXU5566779", "20DV", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, true, null, null, null, null, "SL-015679", "Discharged", null, null, 14900m });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000016"), "HLCUSAI260901610", null, "HLCUBKG2609161", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Lima Foods SAC", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, "PELIM", 2100m, "USD", null, null, null, false, false, "Issued", new DateTime(2026, 9, 28, 6, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Lima, Peru", "Callao (PECLL)", "PECLL", "San Antonio (CLSAI)", "Export", "Importadora Demo SpA", "Departed", "BL", "Cartagena Express", null, "2609S" },
                    { new Guid("11111111-0007-0007-0007-000000000017"), "HLCUARI260901720", null, "HLCUBKG2609172", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "Andes Foods SAC", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "PELIM", 1300m, "USD", null, null, null, false, false, "Issued", new DateTime(2026, 9, 30, 8, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Lima, Peru", "Callao (PECLL)", "PECLL", "Arica (CLARI)", "Export", "Comercial Altiplano SRL", "Departed", "SWB", "Antofagasta Express", null, "2609S" }
                });

            migrationBuilder.InsertData(
                table: "ChargeConcepts",
                columns: new[] { "Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff" },
                values: new object[,]
                {
                    { new Guid("04860673-2222-59b2-d04c-6a971962465b"), "Service", "BL_HOUSE_TRANSMISSION", "CL,BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 260, true, null, null, "Transmisión de BL hijo", false, false },
                    { new Guid("14a10063-2e24-3dad-0f58-d91f07276e71"), "Service", "MATRIX_LATE", "CL,BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 270, true, null, null, "Matriz fuera de plazo", false, false },
                    { new Guid("2ba2ee40-e9b4-20e6-5f43-3597e9b1e3e4"), "Service", "XOM", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 240, true, null, null, "Administración de contenedor (XOM)", false, false },
                    { new Guid("349644e9-cc16-f243-5929-b308f01814d7"), "Service", "SEAL_MANAGEMENT", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 210, true, null, null, "Gestión de sellos", false, false },
                    { new Guid("7cb784b5-5363-d794-f9b9-ba72edd30a69"), "Service", "DROP_OFF", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 230, true, null, null, "Drop Off (devolución en SCL)", false, false },
                    { new Guid("95cfe440-ad17-116d-e382-d8d4925a8450"), "LocalCharge", "VALUATION", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 290, true, null, null, "Valorización", false, false },
                    { new Guid("da68ecd8-b854-1daa-fea9-3c7e06e83e30"), "Service", "EARLY_ARRIVAL", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 220, true, null, null, "Early (ingreso anticipado)", false, false },
                    { new Guid("f27c3d60-1e67-07f7-b8f6-204b0f483bd5"), "LocalCharge", "OPENING", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 280, true, null, null, "Apertura", false, false },
                    { new Guid("fc9e7715-ccf9-3b6a-4c06-744aea94758d"), "Service", "BL_CORRECTION", "CL,BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 250, true, null, null, "Corrección o aclaración de BL", false, false }
                });

            migrationBuilder.InsertData(
                table: "ExchangeRateRecords",
                columns: new[] { "Id", "Approved", "CapturedAt", "ConvertedAmount", "EffectiveDate", "FromCurrency", "Rate", "Source", "SourceAmount", "ToCurrency", "TransactionId", "TransactionType" },
                values: new object[] { new Guid("ff504a00-3987-a4bb-a953-ee9fd8f4765c"), true, new DateTime(2026, 10, 2, 14, 58, 0, 0, DateTimeKind.Utc), 114000m, new DateOnly(2026, 10, 2), "USD", 950m, "DUMMY", 120m, "CLP", new Guid("55555555-000b-000b-000b-000000000014"), "Payment" });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000027"), 12000m, new Guid("11111111-0007-0007-0007-000000000006"), "SEAL_MANAGEMENT", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Gestión de sellos (SRV-20261005-5E1A0002)", true, null, null, "Pending", 2280m, 19m, 14280m },
                    { new Guid("33333333-0009-0009-0009-000000000028"), 45000m, new Guid("11111111-0007-0007-0007-000000000002"), "BL_CORRECTION", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Corrección o aclaración de BL (SRV-20261001-5E1A0003)", true, null, null, "Paid", 8550m, 19m, 53550m }
                });

            migrationBuilder.InsertData(
                table: "MaintainerChangeLogs",
                columns: new[] { "Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue" },
                values: new object[,]
                {
                    { new Guid("0c84def6-4017-1cc8-5cb3-bc6949b15e84"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("23d06884-598b-bc4f-1cef-a28759ac8f70"), "ServiceDefinition", "{\"code\":\"EARLY_ARRIVAL\",\"nameEs\":\"Early\",\"nameEn\":\"Early arrival\",\"descriptionEs\":\"Ingreso anticipado de unidades antes de la apertura del stacking; tarifa por tramos de d\\u00EDas; se presta tras el pago (M3-08).\",\"descriptionEn\":\"Container delivery before the stacking opens; tiered by days early; provided after payment (M3-08).\",\"operations\":\"EXPORT\",\"countries\":\"CL\",\"referenceType\":\"Booking\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"BeforeDeparture\",\"requiresContainers\":true,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022containers\\u0022,\\u0022labelEs\\u0022:\\u0022Contenedores\\u0022,\\u0022labelEn\\u0022:\\u0022Containers\\u0022,\\u0022type\\u0022:\\u0022containers\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022days\\u0022,\\u0022labelEs\\u0022:\\u0022D\\\\u00EDas de anticipaci\\\\u00F3n\\u0022,\\u0022labelEn\\u0022:\\u0022Days early\\u0022,\\u0022type\\u0022:\\u0022number\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:1,\\u0022max\\u0022:30,\\u0022integer\\u0022:true,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022observations\\u0022,\\u0022labelEs\\u0022:\\u0022Observaciones\\u0022,\\u0022labelEn\\u0022:\\u0022Remarks\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"EARLY_ARRIVAL\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":\"days\",\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":false,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"CustomerService\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-on-demand.pay\",\"displayOrder\":30,\"isActive\":true}", null },
                    { new Guid("0fb183cc-89fb-a728-538a-341c8a1ba5cc"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("5d3115fa-f98b-ad3c-43e1-390bdb3261c3"), "ServiceDefinition", "{\"code\":\"VALUATION\",\"nameEs\":\"Valorizaci\\u00F3n\",\"nameEn\":\"Valuation\",\"descriptionEs\":\"Homologaci\\u00F3n de CL-IMP-06 con el modelo est\\u00E1ndar: cargo del sistema de origen con reglas de Nexus. Inactiva: hoy se paga como recargo local.\",\"descriptionEn\":\"CL-IMP-06 mapped to the standard model: source-system charge with Nexus rules. Inactive: currently paid as a local charge.\",\"operations\":\"IMPORT\",\"countries\":\"CL\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":false,\"allowMultiplePerBl\":false,\"inputSchemaJson\":\"[]\",\"billingDataRequired\":false,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"SourceCharge\",\"chargeConceptCode\":\"VALUATION\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":true,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-mandatory.pay\",\"displayOrder\":110,\"isActive\":false}", null },
                    { new Guid("25242d34-dba2-fbd8-3cc7-a482b769d040"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("85efb04f-05d5-2318-3bbb-1149a9593e99"), "Tariff", "{\"conceptCode\":\"MATRIX_LATE\",\"code\":null,\"country\":\"CL\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Matriz fuera de plazo, d\\u00EDas desde el plazo de presentaci\\u00F3n (M3-14)\",\"amount\":0,\"tierUnit\":\"CalendarDays\",\"tierMode\":\"Flat\",\"tiers\":[{\"fromUnit\":0,\"toUnit\":1,\"amount\":50},{\"fromUnit\":2,\"toUnit\":5,\"amount\":100},{\"fromUnit\":6,\"toUnit\":null,\"amount\":200}],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("2db0b575-0067-0cba-ac87-53cb2963e9e5"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("09121ab9-02ec-4885-dda1-1cc481c6ddd3"), "Tariff", "{\"conceptCode\":\"BL_HOUSE_TRANSMISSION\",\"code\":\"PLAZO\",\"country\":\"CL\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Transmisi\\u00F3n de BL hijo dentro de plazo (M3-13)\",\"amount\":35,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("42237288-ed5e-c85a-2efc-d27408e207f2"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("3f963e93-b130-ae94-6ee6-8426587d9197"), "Tariff", "{\"conceptCode\":\"BL_CORRECTION\",\"code\":null,\"country\":\"BO\",\"currency\":\"BOB\",\"containerType\":null,\"description\":\"Correcci\\u00F3n o aclaraci\\u00F3n de BL Bolivia (M3-12)\",\"amount\":300,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("431c7c6a-11ee-5094-96c8-8fc4cf1857e1"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("4c67c42c-973f-bad8-3b14-d35c9df6cc01"), "ServiceDefinition", "{\"code\":\"GATE_IN_RETURN\",\"nameEs\":\"Gate In por devoluci\\u00F3n de unidades\",\"nameEn\":\"Gate In for returned export units\",\"descriptionEs\":\"El cliente selecciona el booking y ve sus unidades; el cargo es el Gate In registrado en el sistema de origen, con las exenciones de Nexus (M3-15, CL-EXP-07).\",\"descriptionEn\":\"The customer selects the booking and sees its units; the charge is the Gate In registered in the source system, with Nexus exemptions (M3-15).\",\"operations\":\"EXPORT\",\"countries\":\"CL\",\"referenceType\":\"Booking\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":true,\"allowMultiplePerBl\":false,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022containers\\u0022,\\u0022labelEs\\u0022:\\u0022Contenedores\\u0022,\\u0022labelEn\\u0022:\\u0022Containers\\u0022,\\u0022type\\u0022:\\u0022containers\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":false,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"SourceCharge\",\"chargeConceptCode\":\"GATE_IN\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":true,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-mandatory.pay\",\"displayOrder\":90,\"isActive\":true}", null },
                    { new Guid("46f9d240-dab8-66bd-d19d-aa9e21346450"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("b023bef2-0e70-2988-f28c-2da684ab098e"), "Tariff", "{\"conceptCode\":\"BL_HOUSE_TRANSMISSION\",\"code\":\"FUERA_PLAZO\",\"country\":\"CL\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Transmisi\\u00F3n de BL hijo fuera de plazo, horas desde el plazo (M3-13)\",\"amount\":0,\"tierUnit\":\"Hours\",\"tierMode\":\"Flat\",\"tiers\":[{\"fromUnit\":0,\"toUnit\":24,\"amount\":60},{\"fromUnit\":25,\"toUnit\":72,\"amount\":120},{\"fromUnit\":73,\"toUnit\":null,\"amount\":250}],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("495338b3-b5bd-0ece-e4f1-2a53619a8bb1"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ea3a5a01-c4ee-76ca-9d51-f9e542075cc7"), "Tariff", "{\"conceptCode\":\"XOM\",\"code\":null,\"country\":\"BO\",\"currency\":\"BOB\",\"containerType\":\"40HC\",\"description\":\"Administraci\\u00F3n de contenedor XOM, 40\\u0027 HC (M3-10)\",\"amount\":520,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("4e72a36d-56b5-10c6-c568-24c15dd2a17e"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("8991b39b-f6dc-fa22-f2cf-cdaae3287f4b"), "ServiceDefinition", "{\"code\":\"DROP_OFF_SCL\",\"nameEs\":\"Drop Off SCL\",\"nameEn\":\"Drop Off SCL\",\"descriptionEs\":\"Devoluci\\u00F3n de contenedores en Santiago: el cliente elige las unidades, acepta la tarifa y el equipo ED aprueba o rechaza (M3-09, CL-IMP-10).\",\"descriptionEn\":\"Container return in Santiago: the customer selects the units, accepts the tariff and the ED team approves or rejects (M3-09).\",\"operations\":\"IMPORT\",\"countries\":\"CL\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"AfterArrival\",\"requiresContainers\":true,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022containers\\u0022,\\u0022labelEs\\u0022:\\u0022Contenedores\\u0022,\\u0022labelEn\\u0022:\\u0022Containers\\u0022,\\u0022type\\u0022:\\u0022containers\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022returnDate\\u0022,\\u0022labelEs\\u0022:\\u0022Fecha de devoluci\\\\u00F3n\\u0022,\\u0022labelEn\\u0022:\\u0022Return date\\u0022,\\u0022type\\u0022:\\u0022date\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022depot\\u0022,\\u0022labelEs\\u0022:\\u0022Dep\\\\u00F3sito de devoluci\\\\u00F3n\\u0022,\\u0022labelEn\\u0022:\\u0022Return depot\\u0022,\\u0022type\\u0022:\\u0022select\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:[{\\u0022value\\u0022:\\u0022SCL_PUDAHUEL\\u0022,\\u0022labelEs\\u0022:\\u0022Dep\\\\u00F3sito Pudahuel\\u0022,\\u0022labelEn\\u0022:\\u0022Pudahuel depot\\u0022},{\\u0022value\\u0022:\\u0022SCL_QUILICURA\\u0022,\\u0022labelEs\\u0022:\\u0022Dep\\\\u00F3sito Quilicura\\u0022,\\u0022labelEn\\u0022:\\u0022Quilicura depot\\u0022},{\\u0022value\\u0022:\\u0022SCL_SAN_BERNARDO\\u0022,\\u0022labelEs\\u0022:\\u0022Dep\\\\u00F3sito San Bernardo\\u0022,\\u0022labelEn\\u0022:\\u0022San Bernardo depot\\u0022}],\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022observations\\u0022,\\u0022labelEs\\u0022:\\u0022Observaciones\\u0022,\\u0022labelEn\\u0022:\\u0022Remarks\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":true,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"DROP_OFF\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerContainer\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":true,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"ED\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"drop-off.request\",\"displayOrder\":40,\"isActive\":true}", null },
                    { new Guid("534111a5-d9fe-9b27-3302-c3faf5ac50c8"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("8f9cd335-f4f6-df32-36b6-94794d2906dc"), "Tariff", "{\"conceptCode\":\"DROP_OFF\",\"code\":null,\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":null,\"description\":\"Drop Off SCL por contenedor (M3-09)\",\"amount\":180000,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("5b857144-a78e-010d-dee9-a7cd0dd2cbf4"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("08f6a148-a021-357f-11e0-9e263e3bc70e"), "Tariff", "{\"conceptCode\":\"SEAL_MANAGEMENT\",\"code\":null,\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":null,\"description\":\"Gesti\\u00F3n de sellos por contenedor (M3-07)\",\"amount\":12000,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("619584fa-1d71-2fc2-9b1b-74cab244ee62"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("7eb0b617-bc72-a0a1-a533-93df4e8d1d6e"), "ServiceDefinition", "{\"code\":\"LATE_ARRIVAL\",\"nameEs\":\"Late Arrival\",\"nameEn\":\"Late Arrival\",\"descriptionEs\":\"Ingreso de unidades despu\\u00E9s del cierre de recepci\\u00F3n; tarifa por tramos de horas de atraso; se presta tras el pago (M3-08, CL-EXP-10).\",\"descriptionEn\":\"Container delivery after the receiving cut-off; tiered by hours late; provided after payment (M3-08).\",\"operations\":\"EXPORT\",\"countries\":\"CL\",\"referenceType\":\"Booking\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"BeforeDeparture\",\"requiresContainers\":true,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022containers\\u0022,\\u0022labelEs\\u0022:\\u0022Contenedores\\u0022,\\u0022labelEn\\u0022:\\u0022Containers\\u0022,\\u0022type\\u0022:\\u0022containers\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022hours\\u0022,\\u0022labelEs\\u0022:\\u0022Horas de atraso respecto del cierre\\u0022,\\u0022labelEn\\u0022:\\u0022Hours after the cut-off\\u0022,\\u0022type\\u0022:\\u0022number\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:1,\\u0022max\\u0022:240,\\u0022integer\\u0022:true,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022observations\\u0022,\\u0022labelEs\\u0022:\\u0022Observaciones\\u0022,\\u0022labelEn\\u0022:\\u0022Remarks\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"LATE_ARRIVAL\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":\"hours\",\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":false,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"CustomerService\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-on-demand.pay\",\"displayOrder\":20,\"isActive\":true}", null },
                    { new Guid("70c99437-5242-1670-7f8d-cc32be458aef"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("1f4a4958-c841-daef-756a-b040b2014e4a"), "ServiceDefinition", "{\"code\":\"XOM\",\"nameEs\":\"Administraci\\u00F3n de contenedor (XOM)\",\"nameEn\":\"Container administration (XOM)\",\"descriptionEs\":\"Cargo por unidad en bolivianos para la operaci\\u00F3n de Bolivia; no se cobra con una excepci\\u00F3n vigente en Nexus (XOM) ni a las unidades del embarcador (SOC) (M3-10, BO-EXP-02, BO-IMP-04).\",\"descriptionEn\":\"Per-unit charge in bolivianos for the Bolivia operation; waived by a Nexus exception (XOM) and for shipper-owned units (M3-10).\",\"operations\":\"IMPORT,EXPORT\",\"countries\":\"BO\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":true,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022containers\\u0022,\\u0022labelEs\\u0022:\\u0022Contenedores\\u0022,\\u0022labelEn\\u0022:\\u0022Containers\\u0022,\\u0022type\\u0022:\\u0022containers\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"XOM\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerContainer\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":false,\"exemptionConcept\":\"XOM\",\"excludeShipperOwnedContainers\":true,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-on-demand.pay\",\"displayOrder\":50,\"isActive\":true}", null },
                    { new Guid("76574e01-7eec-b1c6-fd96-d86cae36ffac"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("62db1148-8639-1e56-909e-ec8eaaa7d035"), "ServiceDefinition", "{\"code\":\"OPENING\",\"nameEs\":\"Apertura\",\"nameEn\":\"Opening\",\"descriptionEs\":\"Homologaci\\u00F3n de CL-IMP-05 con el modelo est\\u00E1ndar: cargo del sistema de origen con reglas de Nexus. Inactiva: hoy se paga como recargo local.\",\"descriptionEn\":\"CL-IMP-05 mapped to the standard model: source-system charge with Nexus rules. Inactive: currently paid as a local charge.\",\"operations\":\"IMPORT\",\"countries\":\"CL\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":false,\"allowMultiplePerBl\":false,\"inputSchemaJson\":\"[]\",\"billingDataRequired\":false,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"SourceCharge\",\"chargeConceptCode\":\"OPENING\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":true,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-mandatory.pay\",\"displayOrder\":100,\"isActive\":false}", null },
                    { new Guid("7ddf473a-2e3b-af3d-0a5c-e2f912d005cf"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("4f0226d1-eabb-3e9f-6355-41589b2ceb4a"), "Tariff", "{\"conceptCode\":\"BL_CORRECTION\",\"code\":null,\"country\":\"CL\",\"currency\":\"CLP\",\"containerType\":null,\"description\":\"Correcci\\u00F3n o aclaraci\\u00F3n de BL (M3-12)\",\"amount\":45000,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("84c4c7c9-2237-1f1c-fc66-3d44058448b1"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("ed9325ae-cb8d-6b70-cfc6-3bfaeaff51e2"), "Tariff", "{\"conceptCode\":\"BL_HOUSE_TRANSMISSION\",\"code\":\"PLAZO\",\"country\":\"BO\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Transmisi\\u00F3n de BL hijo dentro de plazo (M3-13)\",\"amount\":35,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("959121c1-5b6a-a81b-f74c-33d4af71595b"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("cf38fc5e-d7af-910b-aba2-ea8ddde3e9d7"), "ServiceDefinition", "{\"code\":\"MATRIX_LATE\",\"nameEs\":\"Matriz fuera de plazo\",\"nameEn\":\"Late matrix submission\",\"descriptionEs\":\"Cobro por presentar la matriz despu\\u00E9s del plazo (48 horas antes del zarpe mientras Nexus no informe los plazos documentales, M2-10); tarifa por tramos de d\\u00EDas desde el plazo (M3-14).\",\"descriptionEn\":\"Charge for submitting the matrix after the deadline (48 hours before departure until Nexus reports document deadlines, M2-10); tiered by days since the deadline (M3-14).\",\"operations\":\"EXPORT\",\"countries\":\"CL,BO\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":false,\"allowMultiplePerBl\":false,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022observations\\u0022,\\u0022labelEs\\u0022:\\u0022Observaciones\\u0022,\\u0022labelEn\\u0022:\\u0022Remarks\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"MATRIX_LATE\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"VesselDeparture\",\"milestoneOffsetHours\":-48,\"deadlineRuleCode\":null,\"timingRule\":\"LateOnly\",\"taxable\":false,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"None\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-on-demand.pay\",\"displayOrder\":80,\"isActive\":true}", null },
                    { new Guid("a1fbcd01-ec97-6f0b-e0dd-da2db2f77f15"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("6e03d8d0-8670-c1cf-46a4-5248d899b57e"), "ServiceDefinition", "{\"code\":\"BL_HOUSE_TRANSMISSION\",\"nameEs\":\"Transmisi\\u00F3n de BL hijo\",\"nameEn\":\"House BL transmission\",\"descriptionEs\":\"Transmisi\\u00F3n de BL hijo de exportaci\\u00F3n dentro o fuera de plazo: el plazo es el aduanero del BL o de su manifiesto (BL_EMPTY_OUT) o, sin \\u00E9l, el zarpe m\\u00E1s 72 horas; fuera de plazo se cobra por tramos de horas (M3-13).\",\"descriptionEn\":\"Export house BL transmission in time or late: the deadline is the customs one of the BL or its manifest (BL_EMPTY_OUT) or, without it, departure plus 72 hours; late transmissions are tiered by hours (M3-13).\",\"operations\":\"EXPORT\",\"countries\":\"CL,BO\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"AfterDeparture\",\"requiresContainers\":false,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022houseBlNumbers\\u0022,\\u0022labelEs\\u0022:\\u0022N\\\\u00FAmeros de BL hijo\\u0022,\\u0022labelEn\\u0022:\\u0022House BL numbers\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:500,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022observations\\u0022,\\u0022labelEs\\u0022:\\u0022Observaciones\\u0022,\\u0022labelEn\\u0022:\\u0022Remarks\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"BL_HOUSE_TRANSMISSION\",\"tariffCode\":\"PLAZO\",\"lateTariffCode\":\"FUERA_PLAZO\",\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"CustomsDeadline\",\"milestoneOffsetHours\":72,\"deadlineRuleCode\":\"BL_EMPTY_OUT\",\"timingRule\":\"InTimeAndLate\",\"taxable\":false,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"CustomerService\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-on-demand.pay\",\"displayOrder\":70,\"isActive\":true}", null },
                    { new Guid("a6924dd1-4d90-21a1-5a01-037e1f908345"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("37f4f7a0-8da4-c832-bb22-7d2009de5dad"), "ServiceDefinition", "{\"code\":\"BL_CORRECTION\",\"nameEs\":\"Correcci\\u00F3n o aclaraci\\u00F3n de BL\",\"nameEn\":\"BL correction or clarification\",\"descriptionEs\":\"Solicitud y pago de correcciones o aclaraciones del BL, con seguimiento de su estado; Customer Service la atiende tras el pago (M3-12).\",\"descriptionEn\":\"Request and payment of BL corrections or clarifications, with status tracking; handled by Customer Service after payment (M3-12).\",\"operations\":\"IMPORT,EXPORT\",\"countries\":\"CL,BO\",\"referenceType\":\"BL\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"Always\",\"requiresContainers\":false,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022requestType\\u0022,\\u0022labelEs\\u0022:\\u0022Tipo de solicitud\\u0022,\\u0022labelEn\\u0022:\\u0022Request type\\u0022,\\u0022type\\u0022:\\u0022select\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:[{\\u0022value\\u0022:\\u0022CORRECTION\\u0022,\\u0022labelEs\\u0022:\\u0022Correcci\\\\u00F3n\\u0022,\\u0022labelEn\\u0022:\\u0022Correction\\u0022},{\\u0022value\\u0022:\\u0022CLARIFICATION\\u0022,\\u0022labelEs\\u0022:\\u0022Aclaraci\\\\u00F3n\\u0022,\\u0022labelEn\\u0022:\\u0022Clarification\\u0022}],\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022section\\u0022,\\u0022labelEs\\u0022:\\u0022Secci\\\\u00F3n del BL\\u0022,\\u0022labelEn\\u0022:\\u0022BL section\\u0022,\\u0022type\\u0022:\\u0022select\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:[{\\u0022value\\u0022:\\u0022SHIPPER\\u0022,\\u0022labelEs\\u0022:\\u0022Embarcador\\u0022,\\u0022labelEn\\u0022:\\u0022Shipper\\u0022},{\\u0022value\\u0022:\\u0022CONSIGNEE\\u0022,\\u0022labelEs\\u0022:\\u0022Consignatario\\u0022,\\u0022labelEn\\u0022:\\u0022Consignee\\u0022},{\\u0022value\\u0022:\\u0022NOTIFY\\u0022,\\u0022labelEs\\u0022:\\u0022Notificar a\\u0022,\\u0022labelEn\\u0022:\\u0022Notify party\\u0022},{\\u0022value\\u0022:\\u0022CARGO\\u0022,\\u0022labelEs\\u0022:\\u0022Descripci\\\\u00F3n de la carga\\u0022,\\u0022labelEn\\u0022:\\u0022Cargo description\\u0022},{\\u0022value\\u0022:\\u0022CONTAINERS\\u0022,\\u0022labelEs\\u0022:\\u0022Contenedores y sellos\\u0022,\\u0022labelEn\\u0022:\\u0022Containers and seals\\u0022},{\\u0022value\\u0022:\\u0022FREIGHT\\u0022,\\u0022labelEs\\u0022:\\u0022Flete\\u0022,\\u0022labelEn\\u0022:\\u0022Freight\\u0022},{\\u0022value\\u0022:\\u0022OTHER\\u0022,\\u0022labelEs\\u0022:\\u0022Otra\\u0022,\\u0022labelEn\\u0022:\\u0022Other\\u0022}],\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022description\\u0022,\\u0022labelEs\\u0022:\\u0022Detalle de la correcci\\\\u00F3n o aclaraci\\\\u00F3n\\u0022,\\u0022labelEn\\u0022:\\u0022Correction or clarification details\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:2000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022supportingDocument\\u0022,\\u0022labelEs\\u0022:\\u0022Documento de respaldo\\u0022,\\u0022labelEn\\u0022:\\u0022Supporting document\\u0022,\\u0022type\\u0022:\\u0022file\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"BL_CORRECTION\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerRequest\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":true,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"CustomerService\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-on-demand.pay\",\"displayOrder\":60,\"isActive\":true}", null },
                    { new Guid("c5544fec-42c2-acc4-7ada-c458f513372e"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("f69d95b8-610f-9e67-bb63-836e8e6ba3d2"), "Tariff", "{\"conceptCode\":\"MATRIX_LATE\",\"code\":null,\"country\":\"BO\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Matriz fuera de plazo, d\\u00EDas desde el plazo de presentaci\\u00F3n (M3-14)\",\"amount\":0,\"tierUnit\":\"CalendarDays\",\"tierMode\":\"Flat\",\"tiers\":[{\"fromUnit\":0,\"toUnit\":1,\"amount\":50},{\"fromUnit\":2,\"toUnit\":5,\"amount\":100},{\"fromUnit\":6,\"toUnit\":null,\"amount\":200}],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("d880e37a-b4e7-4968-d22c-7722998ad7d2"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("f383edf7-a9c4-fff5-2c56-78c53ce50bc1"), "Tariff", "{\"conceptCode\":\"XOM\",\"code\":null,\"country\":\"BO\",\"currency\":\"BOB\",\"containerType\":null,\"description\":\"Administraci\\u00F3n de contenedor XOM por unidad (M3-10)\",\"amount\":350,\"tierUnit\":\"None\",\"tierMode\":\"Flat\",\"tiers\":[],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("e2b446b6-0654-1934-f54a-a38e2ebd598a"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("14c8c000-da8f-ba14-3e54-52df0949ac66"), "Tariff", "{\"conceptCode\":\"BL_HOUSE_TRANSMISSION\",\"code\":\"FUERA_PLAZO\",\"country\":\"BO\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Transmisi\\u00F3n de BL hijo fuera de plazo, horas desde el plazo (M3-13)\",\"amount\":0,\"tierUnit\":\"Hours\",\"tierMode\":\"Flat\",\"tiers\":[{\"fromUnit\":0,\"toUnit\":24,\"amount\":60},{\"fromUnit\":25,\"toUnit\":72,\"amount\":120},{\"fromUnit\":73,\"toUnit\":null,\"amount\":250}],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("e7efc002-82db-179e-b1f1-ab3f8d91e501"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("7e645de7-824d-4876-2e7e-d433d76e1f92"), "Tariff", "{\"conceptCode\":\"EARLY_ARRIVAL\",\"code\":null,\"country\":\"CL\",\"currency\":\"USD\",\"containerType\":null,\"description\":\"Early por d\\u00EDas de anticipaci\\u00F3n (M3-08)\",\"amount\":0,\"tierUnit\":\"CalendarDays\",\"tierMode\":\"Flat\",\"tiers\":[{\"fromUnit\":1,\"toUnit\":2,\"amount\":80},{\"fromUnit\":3,\"toUnit\":5,\"amount\":150},{\"fromUnit\":6,\"toUnit\":null,\"amount\":250}],\"validFrom\":\"2026-10-01\",\"validTo\":null,\"isActive\":true}", null },
                    { new Guid("e8703b3e-1773-0ee3-ef72-9f591e2ae550"), "Created", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, new Guid("b75305ad-33dc-5c3e-6831-1b8aedfc9756"), "ServiceDefinition", "{\"code\":\"SEAL_MANAGEMENT\",\"nameEs\":\"Gesti\\u00F3n de sellos\",\"nameEn\":\"Seal management\",\"descriptionEs\":\"Ingreso de los datos de sellos del booking; se presta tras el pago (M3-07, CL-EXP-09).\",\"descriptionEn\":\"Seal data entry for the booking; provided after payment (M3-07).\",\"operations\":\"EXPORT\",\"countries\":\"CL\",\"referenceType\":\"Booking\",\"requiredBlStatuses\":null,\"availabilityWindow\":\"BeforeDeparture\",\"requiresContainers\":true,\"allowMultiplePerBl\":true,\"inputSchemaJson\":\"[{\\u0022key\\u0022:\\u0022containers\\u0022,\\u0022labelEs\\u0022:\\u0022Contenedores\\u0022,\\u0022labelEn\\u0022:\\u0022Containers\\u0022,\\u0022type\\u0022:\\u0022containers\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:null,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022sealNumbers\\u0022,\\u0022labelEs\\u0022:\\u0022N\\\\u00FAmeros de sello\\u0022,\\u0022labelEn\\u0022:\\u0022Seal numbers\\u0022,\\u0022type\\u0022:\\u0022text\\u0022,\\u0022required\\u0022:true,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:500,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null},{\\u0022key\\u0022:\\u0022observations\\u0022,\\u0022labelEs\\u0022:\\u0022Observaciones\\u0022,\\u0022labelEn\\u0022:\\u0022Remarks\\u0022,\\u0022type\\u0022:\\u0022textarea\\u0022,\\u0022required\\u0022:false,\\u0022options\\u0022:null,\\u0022min\\u0022:null,\\u0022max\\u0022:null,\\u0022integer\\u0022:false,\\u0022maxLength\\u0022:1000,\\u0022helpEs\\u0022:null,\\u0022helpEn\\u0022:null}]\",\"billingDataRequired\":true,\"tariffAcceptanceRequired\":false,\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"SEAL_MANAGEMENT\",\"tariffCode\":null,\"lateTariffCode\":null,\"quantityMode\":\"PerContainer\",\"measureFieldKey\":null,\"milestone\":\"None\",\"milestoneOffsetHours\":0,\"deadlineRuleCode\":null,\"timingRule\":\"None\",\"taxable\":true,\"exemptionConcept\":null,\"excludeShipperOwnedContainers\":false,\"approvalTeam\":\"None\",\"fulfillmentTeam\":\"CustomerService\",\"requiresOutputDocument\":false,\"actionCode\":\"local-charges-on-demand.pay\",\"displayOrder\":10,\"isActive\":true}", null }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount" },
                values: new object[] { new Guid("55555555-000b-000b-000b-000000000014"), null, 168940m, null, null, null, null, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), "KHIPU_WEBHOOK", "CL", new DateTime(2026, 10, 2, 14, 58, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000010", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "CLP", null, null, null, null, "PAY-20261002-6F5E4D3C", null, null, null, null, null, "Cart", "Importadora Demo SpA", "76123456-7", new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), "KHIPU", "KHIPU", "PAY-20261002-6F5E4D3C", "Cart", "Khipu", "DUMMY-KHIPU-PAY-20261002-6F5E4D3C", "KHP-TXN-8813377", "RCP-20261002-7A8B9C0D", null, null, null, null, "Confirmed", new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), 8550m, 177490m });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[] { new Guid("61285041-197b-46ea-7ea3-57f2c557eba3"), "service-requests.process", "service-requests.process" });

            migrationBuilder.InsertData(
                table: "ServiceDefinitions",
                columns: new[] { "Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule" },
                values: new object[,]
                {
                    { new Guid("1f4a4958-c841-daef-756a-b040b2014e4a"), "local-charges-on-demand.pay", true, "None", "Always", true, "XOM", "XOM", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Per-unit charge in bolivianos for the Bolivia operation; waived by a Nexus exception (XOM) and for shipper-owned units (M3-10).", "Cargo por unidad en bolivianos para la operación de Bolivia; no se cobra con una excepción vigente en Nexus (XOM) ni a las unidades del embarcador (SOC) (M3-10, BO-EXP-02, BO-IMP-04).", 50, true, "XOM", "None", "[{\"key\":\"containers\",\"labelEs\":\"Contenedores\",\"labelEn\":\"Containers\",\"type\":\"containers\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "None", 0, null, null, "Container administration (XOM)", "Administración de contenedor (XOM)", "IMPORT,EXPORT", "Tariff", "PerContainer", "BL", null, true, false, false, null, false, "None" },
                    { new Guid("23d06884-598b-bc4f-1cef-a28759ac8f70"), "local-charges-on-demand.pay", true, "None", "BeforeDeparture", true, "EARLY_ARRIVAL", "EARLY_ARRIVAL", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Container delivery before the stacking opens; tiered by days early; provided after payment (M3-08).", "Ingreso anticipado de unidades antes de la apertura del stacking; tarifa por tramos de días; se presta tras el pago (M3-08).", 30, false, null, "CustomerService", "[{\"key\":\"containers\",\"labelEs\":\"Contenedores\",\"labelEn\":\"Containers\",\"type\":\"containers\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"days\",\"labelEs\":\"D\\u00EDas de anticipaci\\u00F3n\",\"labelEn\":\"Days early\",\"type\":\"number\",\"required\":true,\"options\":null,\"min\":1,\"max\":30,\"integer\":true,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"observations\",\"labelEs\":\"Observaciones\",\"labelEn\":\"Remarks\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, null, "days", "None", 0, null, null, "Early arrival", "Early", "EXPORT", "Tariff", "PerRequest", "Booking", null, true, false, false, null, false, "None" },
                    { new Guid("37f4f7a0-8da4-c832-bb22-7d2009de5dad"), "local-charges-on-demand.pay", true, "None", "Always", true, "BL_CORRECTION", "BL_CORRECTION", "CL,BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Request and payment of BL corrections or clarifications, with status tracking; handled by Customer Service after payment (M3-12).", "Solicitud y pago de correcciones o aclaraciones del BL, con seguimiento de su estado; Customer Service la atiende tras el pago (M3-12).", 60, false, null, "CustomerService", "[{\"key\":\"requestType\",\"labelEs\":\"Tipo de solicitud\",\"labelEn\":\"Request type\",\"type\":\"select\",\"required\":true,\"options\":[{\"value\":\"CORRECTION\",\"labelEs\":\"Correcci\\u00F3n\",\"labelEn\":\"Correction\"},{\"value\":\"CLARIFICATION\",\"labelEs\":\"Aclaraci\\u00F3n\",\"labelEn\":\"Clarification\"}],\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"section\",\"labelEs\":\"Secci\\u00F3n del BL\",\"labelEn\":\"BL section\",\"type\":\"select\",\"required\":true,\"options\":[{\"value\":\"SHIPPER\",\"labelEs\":\"Embarcador\",\"labelEn\":\"Shipper\"},{\"value\":\"CONSIGNEE\",\"labelEs\":\"Consignatario\",\"labelEn\":\"Consignee\"},{\"value\":\"NOTIFY\",\"labelEs\":\"Notificar a\",\"labelEn\":\"Notify party\"},{\"value\":\"CARGO\",\"labelEs\":\"Descripci\\u00F3n de la carga\",\"labelEn\":\"Cargo description\"},{\"value\":\"CONTAINERS\",\"labelEs\":\"Contenedores y sellos\",\"labelEn\":\"Containers and seals\"},{\"value\":\"FREIGHT\",\"labelEs\":\"Flete\",\"labelEn\":\"Freight\"},{\"value\":\"OTHER\",\"labelEs\":\"Otra\",\"labelEn\":\"Other\"}],\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"description\",\"labelEs\":\"Detalle de la correcci\\u00F3n o aclaraci\\u00F3n\",\"labelEn\":\"Correction or clarification details\",\"type\":\"textarea\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":2000,\"helpEs\":null,\"helpEn\":null},{\"key\":\"supportingDocument\",\"labelEs\":\"Documento de respaldo\",\"labelEn\":\"Supporting document\",\"type\":\"file\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "None", 0, null, null, "BL correction or clarification", "Corrección o aclaración de BL", "IMPORT,EXPORT", "Tariff", "PerRequest", "BL", null, false, false, false, null, true, "None" },
                    { new Guid("4c67c42c-973f-bad8-3b14-d35c9df6cc01"), "local-charges-mandatory.pay", false, "None", "Always", false, "GATE_IN", "GATE_IN_RETURN", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "The customer selects the booking and sees its units; the charge is the Gate In registered in the source system, with Nexus exemptions (M3-15).", "El cliente selecciona el booking y ve sus unidades; el cargo es el Gate In registrado en el sistema de origen, con las exenciones de Nexus (M3-15, CL-EXP-07).", 90, false, null, "None", "[{\"key\":\"containers\",\"labelEs\":\"Contenedores\",\"labelEn\":\"Containers\",\"type\":\"containers\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "None", 0, null, null, "Gate In for returned export units", "Gate In por devolución de unidades", "EXPORT", "SourceCharge", "PerRequest", "Booking", null, true, false, false, null, true, "None" },
                    { new Guid("5d3115fa-f98b-ad3c-43e1-390bdb3261c3"), "local-charges-mandatory.pay", false, "None", "Always", false, "VALUATION", "VALUATION", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "CL-IMP-06 mapped to the standard model: source-system charge with Nexus rules. Inactive: currently paid as a local charge.", "Homologación de CL-IMP-06 con el modelo estándar: cargo del sistema de origen con reglas de Nexus. Inactiva: hoy se paga como recargo local.", 110, false, null, "None", "[]", false, null, null, "None", 0, null, null, "Valuation", "Valorización", "IMPORT", "SourceCharge", "PerRequest", "BL", null, false, false, false, null, true, "None" },
                    { new Guid("62db1148-8639-1e56-909e-ec8eaaa7d035"), "local-charges-mandatory.pay", false, "None", "Always", false, "OPENING", "OPENING", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "CL-IMP-05 mapped to the standard model: source-system charge with Nexus rules. Inactive: currently paid as a local charge.", "Homologación de CL-IMP-05 con el modelo estándar: cargo del sistema de origen con reglas de Nexus. Inactiva: hoy se paga como recargo local.", 100, false, null, "None", "[]", false, null, null, "None", 0, null, null, "Opening", "Apertura", "IMPORT", "SourceCharge", "PerRequest", "BL", null, false, false, false, null, true, "None" },
                    { new Guid("6e03d8d0-8670-c1cf-46a4-5248d899b57e"), "local-charges-on-demand.pay", true, "None", "AfterDeparture", true, "BL_HOUSE_TRANSMISSION", "BL_HOUSE_TRANSMISSION", "CL,BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BL_EMPTY_OUT", null, null, "Export house BL transmission in time or late: the deadline is the customs one of the BL or its manifest (BL_EMPTY_OUT) or, without it, departure plus 72 hours; late transmissions are tiered by hours (M3-13).", "Transmisión de BL hijo de exportación dentro o fuera de plazo: el plazo es el aduanero del BL o de su manifiesto (BL_EMPTY_OUT) o, sin él, el zarpe más 72 horas; fuera de plazo se cobra por tramos de horas (M3-13).", 70, false, null, "CustomerService", "[{\"key\":\"houseBlNumbers\",\"labelEs\":\"N\\u00FAmeros de BL hijo\",\"labelEn\":\"House BL numbers\",\"type\":\"text\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":500,\"helpEs\":null,\"helpEn\":null},{\"key\":\"observations\",\"labelEs\":\"Observaciones\",\"labelEn\":\"Remarks\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, "FUERA_PLAZO", null, "CustomsDeadline", 72, null, null, "House BL transmission", "Transmisión de BL hijo", "EXPORT", "Tariff", "PerRequest", "BL", null, false, false, false, "PLAZO", false, "InTimeAndLate" },
                    { new Guid("7eb0b617-bc72-a0a1-a533-93df4e8d1d6e"), "local-charges-on-demand.pay", true, "None", "BeforeDeparture", true, "LATE_ARRIVAL", "LATE_ARRIVAL", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Container delivery after the receiving cut-off; tiered by hours late; provided after payment (M3-08).", "Ingreso de unidades después del cierre de recepción; tarifa por tramos de horas de atraso; se presta tras el pago (M3-08, CL-EXP-10).", 20, false, null, "CustomerService", "[{\"key\":\"containers\",\"labelEs\":\"Contenedores\",\"labelEn\":\"Containers\",\"type\":\"containers\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"hours\",\"labelEs\":\"Horas de atraso respecto del cierre\",\"labelEn\":\"Hours after the cut-off\",\"type\":\"number\",\"required\":true,\"options\":null,\"min\":1,\"max\":240,\"integer\":true,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"observations\",\"labelEs\":\"Observaciones\",\"labelEn\":\"Remarks\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, null, "hours", "None", 0, null, null, "Late Arrival", "Late Arrival", "EXPORT", "Tariff", "PerRequest", "Booking", null, true, false, false, null, false, "None" },
                    { new Guid("8991b39b-f6dc-fa22-f2cf-cdaae3287f4b"), "drop-off.request", true, "ED", "AfterArrival", true, "DROP_OFF", "DROP_OFF_SCL", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Container return in Santiago: the customer selects the units, accepts the tariff and the ED team approves or rejects (M3-09).", "Devolución de contenedores en Santiago: el cliente elige las unidades, acepta la tarifa y el equipo ED aprueba o rechaza (M3-09, CL-IMP-10).", 40, false, null, "None", "[{\"key\":\"containers\",\"labelEs\":\"Contenedores\",\"labelEn\":\"Containers\",\"type\":\"containers\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"returnDate\",\"labelEs\":\"Fecha de devoluci\\u00F3n\",\"labelEn\":\"Return date\",\"type\":\"date\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"depot\",\"labelEs\":\"Dep\\u00F3sito de devoluci\\u00F3n\",\"labelEn\":\"Return depot\",\"type\":\"select\",\"required\":true,\"options\":[{\"value\":\"SCL_PUDAHUEL\",\"labelEs\":\"Dep\\u00F3sito Pudahuel\",\"labelEn\":\"Pudahuel depot\"},{\"value\":\"SCL_QUILICURA\",\"labelEs\":\"Dep\\u00F3sito Quilicura\",\"labelEn\":\"Quilicura depot\"},{\"value\":\"SCL_SAN_BERNARDO\",\"labelEs\":\"Dep\\u00F3sito San Bernardo\",\"labelEn\":\"San Bernardo depot\"}],\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"observations\",\"labelEs\":\"Observaciones\",\"labelEn\":\"Remarks\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "None", 0, null, null, "Drop Off SCL", "Drop Off SCL", "IMPORT", "Tariff", "PerContainer", "BL", null, true, false, true, null, true, "None" },
                    { new Guid("b75305ad-33dc-5c3e-6831-1b8aedfc9756"), "local-charges-on-demand.pay", true, "None", "BeforeDeparture", true, "SEAL_MANAGEMENT", "SEAL_MANAGEMENT", "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Seal data entry for the booking; provided after payment (M3-07).", "Ingreso de los datos de sellos del booking; se presta tras el pago (M3-07, CL-EXP-09).", 10, false, null, "CustomerService", "[{\"key\":\"containers\",\"labelEs\":\"Contenedores\",\"labelEn\":\"Containers\",\"type\":\"containers\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":null,\"helpEs\":null,\"helpEn\":null},{\"key\":\"sealNumbers\",\"labelEs\":\"N\\u00FAmeros de sello\",\"labelEn\":\"Seal numbers\",\"type\":\"text\",\"required\":true,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":500,\"helpEs\":null,\"helpEn\":null},{\"key\":\"observations\",\"labelEs\":\"Observaciones\",\"labelEn\":\"Remarks\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "None", 0, null, null, "Seal management", "Gestión de sellos", "EXPORT", "Tariff", "PerContainer", "Booking", null, true, false, false, null, true, "None" },
                    { new Guid("cf38fc5e-d7af-910b-aba2-ea8ddde3e9d7"), "local-charges-on-demand.pay", false, "None", "Always", true, "MATRIX_LATE", "MATRIX_LATE", "CL,BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, "Charge for submitting the matrix after the deadline (48 hours before departure until Nexus reports document deadlines, M2-10); tiered by days since the deadline (M3-14).", "Cobro por presentar la matriz después del plazo (48 horas antes del zarpe mientras Nexus no informe los plazos documentales, M2-10); tarifa por tramos de días desde el plazo (M3-14).", 80, false, null, "None", "[{\"key\":\"observations\",\"labelEs\":\"Observaciones\",\"labelEn\":\"Remarks\",\"type\":\"textarea\",\"required\":false,\"options\":null,\"min\":null,\"max\":null,\"integer\":false,\"maxLength\":1000,\"helpEs\":null,\"helpEn\":null}]", true, null, null, "VesselDeparture", -48, null, null, "Late matrix submission", "Matriz fuera de plazo", "EXPORT", "Tariff", "PerRequest", "BL", null, false, false, false, null, false, "LateOnly" }
                });

            migrationBuilder.InsertData(
                table: "Tariffs",
                columns: new[] { "Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo" },
                values: new object[,]
                {
                    { new Guid("08f6a148-a021-357f-11e0-9e263e3bc70e"), 12000m, null, "SEAL_MANAGEMENT", null, "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Gestión de sellos por contenedor (M3-07)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("09121ab9-02ec-4885-dda1-1cc481c6ddd3"), 35m, "PLAZO", "BL_HOUSE_TRANSMISSION", null, "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Transmisión de BL hijo dentro de plazo (M3-13)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("14c8c000-da8f-ba14-3e54-52df0949ac66"), 0m, "FUERA_PLAZO", "BL_HOUSE_TRANSMISSION", null, "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Transmisión de BL hijo fuera de plazo, horas desde el plazo (M3-13)", true, null, null, "Flat", "Hours", new DateOnly(2026, 10, 1), null },
                    { new Guid("3f963e93-b130-ae94-6ee6-8426587d9197"), 300m, null, "BL_CORRECTION", null, "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Corrección o aclaración de BL Bolivia (M3-12)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("4f0226d1-eabb-3e9f-6355-41589b2ceb4a"), 45000m, null, "BL_CORRECTION", null, "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Corrección o aclaración de BL (M3-12)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("7e645de7-824d-4876-2e7e-d433d76e1f92"), 0m, null, "EARLY_ARRIVAL", null, "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Early por días de anticipación (M3-08)", true, null, null, "Flat", "CalendarDays", new DateOnly(2026, 10, 1), null },
                    { new Guid("85efb04f-05d5-2318-3bbb-1149a9593e99"), 0m, null, "MATRIX_LATE", null, "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Matriz fuera de plazo, días desde el plazo de presentación (M3-14)", true, null, null, "Flat", "CalendarDays", new DateOnly(2026, 10, 1), null },
                    { new Guid("8f9cd335-f4f6-df32-36b6-94794d2906dc"), 180000m, null, "DROP_OFF", null, "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Drop Off SCL por contenedor (M3-09)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("b023bef2-0e70-2988-f28c-2da684ab098e"), 0m, "FUERA_PLAZO", "BL_HOUSE_TRANSMISSION", null, "CL", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Transmisión de BL hijo fuera de plazo, horas desde el plazo (M3-13)", true, null, null, "Flat", "Hours", new DateOnly(2026, 10, 1), null },
                    { new Guid("ea3a5a01-c4ee-76ca-9d51-f9e542075cc7"), 520m, null, "XOM", "40HC", "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Administración de contenedor XOM, 40' HC (M3-10)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("ed9325ae-cb8d-6b70-cfc6-3bfaeaff51e2"), 35m, "PLAZO", "BL_HOUSE_TRANSMISSION", null, "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Transmisión de BL hijo dentro de plazo (M3-13)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("f383edf7-a9c4-fff5-2c56-78c53ce50bc1"), 350m, null, "XOM", null, "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Administración de contenedor XOM por unidad (M3-10)", true, null, null, "Flat", "None", new DateOnly(2026, 10, 1), null },
                    { new Guid("f69d95b8-610f-9e67-bb63-836e8e6ba3d2"), 0m, null, "MATRIX_LATE", null, "BO", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Matriz fuera de plazo, días desde el plazo de presentación (M3-14)", true, null, null, "Flat", "CalendarDays", new DateOnly(2026, 10, 1), null }
                });

            migrationBuilder.InsertData(
                table: "WarehouseChangeBatches",
                columns: new[] { "Id", "ClientId", "CompletedAt", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "FailedItems", "ModifiedAt", "ModifiedBy", "ProcessedItems", "RequestedByUserId", "StartedAt", "Status", "SucceededItems", "TotalItems" },
                values: new object[] { new Guid("99999999-0020-0020-0020-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 3, 9, 0, 6, 0, DateTimeKind.Utc), new DateTime(2026, 10, 3, 9, 0, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", null, null, 0, null, null, 1, new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new DateTime(2026, 10, 3, 9, 0, 5, 0, DateTimeKind.Utc), "Completed", 1, 1 });

            migrationBuilder.InsertData(
                table: "WarehouseChanges",
                columns: new[] { "Id", "Amount", "BatchId", "BillOfLadingId", "CompletedAt", "ContainerNumber", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "EntitlementReference", "EntitlementSource", "FromWarehouse", "IsFree", "ModifiedAt", "ModifiedBy", "RequestedByClientId", "RequestedByUserId", "Status", "TariffCode", "TariffSource", "ToWarehouse" },
                values: new object[,]
                {
                    { new Guid("99999999-000f-000f-000f-000000000003"), 9940m, null, new Guid("11111111-0007-0007-0007-000000000009"), new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), null, "CL", new DateTime(2026, 10, 2, 14, 30, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", "CLP", null, null, null, null, "STI San Antonio - Patio B", false, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Completed", "KTE", "PORTAL", "Bodega Lo Espejo" },
                    { new Guid("99999999-000f-000f-000f-000000000004"), 0m, new Guid("99999999-0020-0020-0020-000000000001"), new Guid("11111111-0007-0007-0007-000000000010"), new DateTime(2026, 10, 3, 9, 0, 6, 0, DateTimeKind.Utc), null, "CL", new DateTime(2026, 10, 3, 9, 0, 6, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "RULE:eeeeeeee-0015-0015-0015-000000000001", "PORTAL", "N/D", true, null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Completed", null, null, "Bodega Central Santiago" }
                });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000019"), new Guid("11111111-0007-0007-0007-000000000016"), "HLXU2609161", "40RF", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-260916", "OnBoard", null, null, 25400m },
                    { new Guid("22222222-0008-0008-0008-000000000020"), new Guid("11111111-0007-0007-0007-000000000016"), "HLXU2609162", "20DV", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-260917", "OnBoard", null, null, 16100m },
                    { new Guid("22222222-0008-0008-0008-000000000021"), new Guid("11111111-0007-0007-0007-000000000017"), "HLXU2609171", "20DV", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, false, null, null, null, null, "SL-260918", "OnBoard", null, null, 15800m }
                });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[] { new Guid("33333333-0009-0009-0009-000000000029"), 120m, new Guid("11111111-0007-0007-0007-000000000016"), "BL_HOUSE_TRANSMISSION", new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "USD", null, null, "Transmisión de BL hijo (SRV-20261002-5E1A0004)", false, null, null, "Paid", 0m, 0m, 120m });

            migrationBuilder.InsertData(
                table: "PaymentDetails",
                columns: new[] { "Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount" },
                values: new object[,]
                {
                    { new Guid("2d66e7bf-d1f8-ee1d-5010-79747e017882"), null, 45000m, new Guid("11111111-0007-0007-0007-000000000002"), "Importadora Demo SpA", "76123456-7", "HLCUVAL250200456", "HLCUBKG2502004", "BL_CORRECTION", "CLP", "Corrección o aclaración de BL (SRV-20261001-5E1A0003)", null, "LocalCharge", null, 53550m, "CLP", new Guid("55555555-000b-000b-000b-000000000014"), new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-0009-0009-0009-000000000028"), 8550m },
                    { new Guid("30ada185-078c-2fbf-3a02-2812eb08a5a0"), null, 114000m, new Guid("11111111-0007-0007-0007-000000000016"), "Importadora Demo SpA", "76123456-7", "HLCUSAI260901610", "HLCUBKG2609161", "BL_HOUSE_TRANSMISSION", "CLP", "Transmisión de BL hijo (SRV-20261002-5E1A0004)", 950m, "LocalCharge", null, 120m, "USD", new Guid("55555555-000b-000b-000b-000000000014"), new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-0009-0009-0009-000000000029"), 0m },
                    { new Guid("a832ba20-d7cd-be95-bd38-7c02c2b76c51"), null, 9940m, new Guid("11111111-0007-0007-0007-000000000009"), "Importadora Demo SpA", "76123456-7", "HLCUSAI260400910", "HLCUBKG2604091", "WAREHOUSE_CHANGE", "CLP", "Cambio de almacén STI San Antonio - Patio B → Bodega Lo Espejo", null, "WarehouseChange", null, 9940m, "CLP", new Guid("55555555-000b-000b-000b-000000000014"), new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), new Guid("99999999-000f-000f-000f-000000000003"), 0m }
                });

            migrationBuilder.InsertData(
                table: "PaymentStatusChanges",
                columns: new[] { "Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus" },
                values: new object[,]
                {
                    { new Guid("4b5dd8b9-21c3-ce5e-3bba-5c3c17ec632f"), new DateTime(2026, 10, 2, 14, 58, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, new Guid("55555555-000b-000b-000b-000000000014"), null, "Pending" },
                    { new Guid("9bfbb3d4-412d-6600-9482-ef7f1e64220f"), new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), "KHIPU_WEBHOOK", null, "Processing", new Guid("55555555-000b-000b-000b-000000000014"), null, "Confirmed" },
                    { new Guid("a0b9ed1f-e4ac-fe81-28be-77676bf235aa"), new DateTime(2026, 10, 2, 14, 58, 0, 0, DateTimeKind.Utc), "SYSTEM", null, "Pending", new Guid("55555555-000b-000b-000b-000000000014"), "Initiated in Khipu", "Processing" }
                });

            migrationBuilder.InsertData(
                table: "ServiceRequests",
                columns: new[] { "Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0021-0021-0021-000000000001"), null, 180000m, null, "ED", new Guid("11111111-0007-0007-0007-000000000001"), "Importación y distribución", "Av. Apoquindo 4500, Las Condes, Santiago", "facturacion@importadorademo.cl", "Importadora Demo SpA", "76123456-7", "HLCUVAL250100123", "HLCUBKG2501001", null, "DROP_OFF", null, "HLXU1234567", "CL", new DateTime(2026, 10, 5, 13, 55, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", "CLP", "DROP_OFF_SCL", new Guid("8991b39b-f6dc-fa22-f2cf-cdaae3287f4b"), null, null, null, "{\"containers\":[\"HLXU1234567\"],\"returnDate\":\"2026-10-09\",\"depot\":\"SCL_PUDAHUEL\"}", false, null, null, null, null, null, null, "IMPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "{\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"DROP_OFF\",\"requiresPayment\":true,\"amount\":180000,\"taxAmount\":34200,\"totalAmount\":214200,\"currency\":\"CLP\",\"taxRate\":19,\"quantity\":1,\"tierUnit\":null,\"measuredUnits\":null,\"timing\":\"NotApplicable\",\"milestoneAt\":null,\"milestoneSource\":null,\"tariffId\":\"8f9cd335-f4f6-df32-36b6-94794d2906dc\",\"tariffCode\":null,\"tariffSource\":\"PORTAL\",\"isExempt\":false,\"exemptionReference\":null,\"excludedContainers\":[],\"lines\":[{\"containerNumber\":\"HLXU1234567\",\"containerType\":\"40HC\",\"amount\":180000,\"tariffCode\":null,\"breakdown\":[]}],\"sourceCharges\":[],\"timeZone\":\"America/Santiago\",\"quotedAt\":\"2026-10-05T14:00:00Z\"}", 1, new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), null, "SRV-20261005-5E1A0001", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, "PendingApproval", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), null, new Guid("8f9cd335-f4f6-df32-36b6-94794d2906dc"), "PORTAL", 34200m, null, 3, "NotApplicable", 214200m },
                    { new Guid("ffffffff-0021-0021-0021-000000000002"), null, 12000m, null, null, new Guid("11111111-0007-0007-0007-000000000006"), "Importación y distribución", "Av. Apoquindo 4500, Las Condes, Santiago", "facturacion@importadorademo.cl", "Importadora Demo SpA", "76123456-7", "HLCUSAI260300610", "HLCUBKG2603061", null, "SEAL_MANAGEMENT", null, "HLXU2023001", "CL", new DateTime(2026, 10, 5, 14, 57, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", "CLP", "SEAL_MANAGEMENT", new Guid("b75305ad-33dc-5c3e-6831-1b8aedfc9756"), null, null, null, "{\"containers\":[\"HLXU2023001\"],\"sealNumbers\":\"HLS-889120\"}", false, null, null, null, null, null, null, "EXPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "{\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"SEAL_MANAGEMENT\",\"requiresPayment\":true,\"amount\":12000,\"taxAmount\":2280,\"totalAmount\":14280,\"currency\":\"CLP\",\"taxRate\":19,\"quantity\":1,\"tierUnit\":null,\"measuredUnits\":null,\"timing\":\"NotApplicable\",\"milestoneAt\":null,\"milestoneSource\":null,\"tariffId\":\"08f6a148-a021-357f-11e0-9e263e3bc70e\",\"tariffCode\":null,\"tariffSource\":\"PORTAL\",\"isExempt\":false,\"exemptionReference\":null,\"excludedContainers\":[],\"lines\":[{\"containerNumber\":\"HLXU2023001\",\"containerType\":\"40RF\",\"amount\":12000,\"tariffCode\":null,\"breakdown\":[]}],\"sourceCharges\":[],\"timeZone\":\"America/Santiago\",\"quotedAt\":\"2026-10-05T15:00:00Z\"}", 1, new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), null, "SRV-20261005-5E1A0002", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, "PendingPayment", new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), null, null, new Guid("08f6a148-a021-357f-11e0-9e263e3bc70e"), "PORTAL", 2280m, null, 3, "NotApplicable", 14280m },
                    { new Guid("ffffffff-0021-0021-0021-000000000003"), null, 45000m, null, "CustomerService", new Guid("11111111-0007-0007-0007-000000000002"), "Importación y distribución", "Av. Apoquindo 4500, Las Condes, Santiago", "facturacion@importadorademo.cl", "Importadora Demo SpA", "76123456-7", "HLCUVAL250200456", "HLCUBKG2502004", null, "BL_CORRECTION", null, null, "CL", new DateTime(2026, 10, 1, 12, 50, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", "CLP", "BL_CORRECTION", new Guid("37f4f7a0-8da4-c832-bb22-7d2009de5dad"), null, null, null, "{\"requestType\":\"CORRECTION\",\"section\":\"CONSIGNEE\",\"description\":\"Corregir la dirección del consignatario: Av. Apoquindo 4500, Las Condes, Santiago.\"}", false, null, null, null, null, null, null, "IMPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), new Guid("55555555-000b-000b-000b-000000000014"), "{\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"BL_CORRECTION\",\"requiresPayment\":true,\"amount\":45000,\"taxAmount\":8550,\"totalAmount\":53550,\"currency\":\"CLP\",\"taxRate\":19,\"quantity\":1,\"tierUnit\":null,\"measuredUnits\":null,\"timing\":\"NotApplicable\",\"milestoneAt\":null,\"milestoneSource\":null,\"tariffId\":\"4f0226d1-eabb-3e9f-6355-41589b2ceb4a\",\"tariffCode\":null,\"tariffSource\":\"PORTAL\",\"isExempt\":false,\"exemptionReference\":null,\"excludedContainers\":[],\"lines\":[{\"containerNumber\":null,\"containerType\":null,\"amount\":45000,\"tariffCode\":null,\"breakdown\":[]}],\"sourceCharges\":[],\"timeZone\":\"America/Santiago\",\"quotedAt\":\"2026-10-01T13:00:00Z\"}", 1, new DateTime(2026, 10, 1, 13, 0, 0, 0, DateTimeKind.Utc), null, "SRV-20261001-5E1A0003", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, "InProgress", new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 1, 13, 0, 0, 0, DateTimeKind.Utc), null, null, new Guid("4f0226d1-eabb-3e9f-6355-41589b2ceb4a"), "PORTAL", 8550m, null, 5, "NotApplicable", 53550m },
                    { new Guid("ffffffff-0021-0021-0021-000000000004"), null, 120m, null, null, new Guid("11111111-0007-0007-0007-000000000016"), "Importación y distribución", "Av. Apoquindo 4500, Las Condes, Santiago", "facturacion@importadorademo.cl", "Importadora Demo SpA", "76123456-7", "HLCUSAI260901610", "HLCUBKG2609161", null, "BL_HOUSE_TRANSMISSION", new DateTime(2026, 10, 3, 10, 0, 0, 0, DateTimeKind.Utc), null, "CL", new DateTime(2026, 10, 2, 11, 56, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", "USD", "BL_HOUSE_TRANSMISSION", new Guid("6e03d8d0-8670-c1cf-46a4-5248d899b57e"), null, null, null, "{\"houseBlNumbers\":\"HLCUSAI26090161A, HLCUSAI26090161B\"}", false, 36, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ETD", null, null, null, "EXPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), new Guid("55555555-000b-000b-000b-000000000014"), "{\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"BL_HOUSE_TRANSMISSION\",\"requiresPayment\":true,\"amount\":120,\"taxAmount\":0,\"totalAmount\":120,\"currency\":\"USD\",\"taxRate\":0,\"quantity\":1,\"tierUnit\":\"Hours\",\"measuredUnits\":36,\"timing\":\"Late\",\"milestoneAt\":\"2026-10-01T00:00:00Z\",\"milestoneSource\":\"ETD\",\"tariffId\":\"b023bef2-0e70-2988-f28c-2da684ab098e\",\"tariffCode\":\"FUERA_PLAZO\",\"tariffSource\":\"PORTAL\",\"isExempt\":false,\"exemptionReference\":null,\"excludedContainers\":[],\"lines\":[{\"containerNumber\":null,\"containerType\":null,\"amount\":120,\"tariffCode\":\"FUERA_PLAZO\",\"breakdown\":[{\"fromUnit\":25,\"toUnit\":72,\"units\":1,\"unitAmount\":120,\"amount\":120}]}],\"sourceCharges\":[],\"timeZone\":\"America/Santiago\",\"quotedAt\":\"2026-10-02T12:00:00Z\"}", 1, new DateTime(2026, 10, 2, 12, 0, 0, 0, DateTimeKind.Utc), null, "SRV-20261002-5E1A0004", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "BL hijo transmitido y aceptado por Aduana.", "Completed", new DateTime(2026, 10, 3, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 2, 12, 0, 0, 0, DateTimeKind.Utc), null, "FUERA_PLAZO", new Guid("b023bef2-0e70-2988-f28c-2da684ab098e"), "PORTAL", 0m, "Hours", 6, "Late", 120m },
                    { new Guid("ffffffff-0021-0021-0021-000000000005"), null, 180000m, null, null, new Guid("11111111-0007-0007-0007-000000000009"), "Importación y distribución", "Av. Apoquindo 4500, Las Condes, Santiago", "facturacion@importadorademo.cl", "Importadora Demo SpA", "76123456-7", "HLCUSAI260400910", "HLCUBKG2604091", null, "DROP_OFF", null, "HLXU3034001", "CL", new DateTime(2026, 10, 3, 9, 54, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", "CLP", "DROP_OFF_SCL", new Guid("8991b39b-f6dc-fa22-f2cf-cdaae3287f4b"), null, null, null, "{\"containers\":[\"HLXU3034001\"],\"returnDate\":\"2026-10-06\",\"depot\":\"SCL_QUILICURA\"}", false, null, null, null, null, null, null, "IMPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "{\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"DROP_OFF\",\"requiresPayment\":true,\"amount\":180000,\"taxAmount\":34200,\"totalAmount\":214200,\"currency\":\"CLP\",\"taxRate\":19,\"quantity\":1,\"tierUnit\":null,\"measuredUnits\":null,\"timing\":\"NotApplicable\",\"milestoneAt\":null,\"milestoneSource\":null,\"tariffId\":\"8f9cd335-f4f6-df32-36b6-94794d2906dc\",\"tariffCode\":null,\"tariffSource\":\"PORTAL\",\"isExempt\":false,\"exemptionReference\":null,\"excludedContainers\":[],\"lines\":[{\"containerNumber\":\"HLXU3034001\",\"containerType\":\"40HC\",\"amount\":180000,\"tariffCode\":null,\"breakdown\":[]}],\"sourceCharges\":[],\"timeZone\":\"America/Santiago\",\"quotedAt\":\"2026-10-03T10:00:00Z\"}", 1, new DateTime(2026, 10, 3, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 3, 16, 0, 0, 0, DateTimeKind.Utc), "SRV-20261003-5E1A0005", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "El depósito Quilicura no recibe unidades 40HC esta semana; solicite la devolución en Pudahuel.", "Rejected", new DateTime(2026, 10, 3, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 3, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 3, 10, 0, 0, 0, DateTimeKind.Utc), null, new Guid("8f9cd335-f4f6-df32-36b6-94794d2906dc"), "PORTAL", 34200m, null, 4, "NotApplicable", 214200m },
                    { new Guid("ffffffff-0021-0021-0021-000000000006"), null, 0m, null, null, new Guid("11111111-0007-0007-0007-000000000006"), "Importación y distribución", "Av. Apoquindo 4500, Las Condes, Santiago", "facturacion@importadorademo.cl", "Importadora Demo SpA", "76123456-7", "HLCUSAI260300610", "HLCUBKG2603061", new DateTime(2026, 10, 4, 11, 20, 0, 0, DateTimeKind.Utc), null, null, "HLXU2023001", "CL", new DateTime(2026, 10, 4, 11, 0, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", null, "LATE_ARRIVAL", new Guid("7eb0b617-bc72-a0a1-a533-93df4e8d1d6e"), null, null, null, "{\"containers\":[\"HLXU2023001\"],\"hours\":30}", false, null, null, null, null, null, null, "EXPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, null, 1, null, null, "SRV-20261004-5E1A0006", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, "Cancelled", new DateTime(2026, 10, 4, 11, 20, 0, 0, DateTimeKind.Utc), null, null, null, null, null, 0m, null, 2, null, 0m },
                    { new Guid("ffffffff-0021-0021-0021-000000000007"), null, 0m, null, null, new Guid("11111111-0007-0007-0007-000000000005"), "Comercio exterior", "Av. Arce 2631, La Paz", "facturacion@altiplano.bo", "Comercial Altiplano SRL", "1023456017", "HLCUIQQ260200078", "HLCUBKG2602078", null, "XOM", new DateTime(2026, 10, 4, 15, 0, 0, 0, DateTimeKind.Utc), "HLXU5566779", "BO", new DateTime(2026, 10, 4, 14, 58, 0, 0, DateTimeKind.Utc), "demo@altiplano.bo", null, "XOM", new Guid("1f4a4958-c841-daef-756a-b040b2014e4a"), null, null, "SOC:HLXU5566779", "{\"containers\":[\"HLXU5566779\"]}", true, null, null, null, null, null, null, "IMPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, "{\"pricingMode\":\"Tariff\",\"chargeConceptCode\":\"XOM\",\"requiresPayment\":false,\"amount\":0,\"taxAmount\":0,\"totalAmount\":0,\"currency\":null,\"taxRate\":0,\"quantity\":0,\"tierUnit\":null,\"measuredUnits\":null,\"timing\":\"NotApplicable\",\"milestoneAt\":null,\"milestoneSource\":null,\"tariffId\":null,\"tariffCode\":null,\"tariffSource\":null,\"isExempt\":true,\"exemptionReference\":\"SOC:HLXU5566779\",\"excludedContainers\":[\"HLXU5566779\"],\"lines\":[],\"sourceCharges\":[],\"timeZone\":\"America/La_Paz\",\"quotedAt\":\"2026-10-04T15:00:00Z\"}", 0, new DateTime(2026, 10, 4, 15, 0, 0, 0, DateTimeKind.Utc), null, "SRV-20261004-5E1A0007", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), null, "Completed", new DateTime(2026, 10, 4, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 4, 15, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, 0m, null, 3, "NotApplicable", 0m },
                    { new Guid("ffffffff-0021-0021-0021-000000000008"), null, 0m, null, null, new Guid("11111111-0007-0007-0007-000000000016"), "Importación y distribución", "Av. Apoquindo 4500, Las Condes, Santiago", "facturacion@importadorademo.cl", "Importadora Demo SpA", "76123456-7", "HLCUSAI260901610", "HLCUBKG2609161", null, null, null, null, "CL", new DateTime(2026, 10, 5, 18, 0, 0, 0, DateTimeKind.Utc), "demo@importadorademo.cl", null, "MATRIX_LATE", new Guid("cf38fc5e-d7af-910b-aba2-ea8ddde3e9d7"), null, null, null, "{\"observations\":\"Matriz enviada por correo el 29-09.\"}", false, null, null, null, null, null, null, "EXPORT", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, null, 1, null, null, "SRV-20261005-5E1A0008", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, "Draft", new DateTime(2026, 10, 5, 18, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, 0m, null, 1, null, 0m }
                });

            migrationBuilder.InsertData(
                table: "ShipmentRoles",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source" },
                values: new object[,]
                {
                    { new Guid("075936e7-41be-53b0-1290-d3970873bf0f"), new Guid("11111111-0007-0007-0007-000000000017"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Shipper", "Seed" },
                    { new Guid("ef31b036-2222-2f88-9a2f-85178328013c"), new Guid("11111111-0007-0007-0007-000000000016"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 10, 1, 12, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "Shipper", "Seed" }
                });

            migrationBuilder.InsertData(
                table: "TariffTiers",
                columns: new[] { "Id", "Amount", "FromUnit", "TariffId", "ToUnit" },
                values: new object[,]
                {
                    { new Guid("1115e1cb-e5e3-af7b-ce32-0b3cd2bf6644"), 200m, 6, new Guid("85efb04f-05d5-2318-3bbb-1149a9593e99"), null },
                    { new Guid("36e03912-6741-6aaa-b1df-6030fc077295"), 80m, 1, new Guid("7e645de7-824d-4876-2e7e-d433d76e1f92"), 2 },
                    { new Guid("3a54e555-290b-7035-3a46-d48779e9e782"), 60m, 0, new Guid("14c8c000-da8f-ba14-3e54-52df0949ac66"), 24 },
                    { new Guid("59813e09-2340-3be1-6176-a8c729aad23d"), 60m, 0, new Guid("b023bef2-0e70-2988-f28c-2da684ab098e"), 24 },
                    { new Guid("5e0f0096-0f79-5f68-c614-61a6cf3cb064"), 250m, 73, new Guid("b023bef2-0e70-2988-f28c-2da684ab098e"), null },
                    { new Guid("63dcf8d2-9418-c776-3498-1f4c7ea28c6e"), 120m, 25, new Guid("b023bef2-0e70-2988-f28c-2da684ab098e"), 72 },
                    { new Guid("7922f54d-8324-e8a7-61ff-16a15e0f6ccc"), 50m, 0, new Guid("f69d95b8-610f-9e67-bb63-836e8e6ba3d2"), 1 },
                    { new Guid("82204082-2cb6-314d-5720-7d64f6f6d063"), 100m, 2, new Guid("85efb04f-05d5-2318-3bbb-1149a9593e99"), 5 },
                    { new Guid("86950eae-1c4a-4602-f867-0be816597eea"), 100m, 2, new Guid("f69d95b8-610f-9e67-bb63-836e8e6ba3d2"), 5 },
                    { new Guid("8fb701ca-e302-16aa-bb3f-9d088b224703"), 250m, 6, new Guid("7e645de7-824d-4876-2e7e-d433d76e1f92"), null },
                    { new Guid("9989fc8b-7c73-ac18-3ddc-456af0138636"), 120m, 25, new Guid("14c8c000-da8f-ba14-3e54-52df0949ac66"), 72 },
                    { new Guid("a606e47d-f958-2e06-61cb-5669ff5e2329"), 200m, 6, new Guid("f69d95b8-610f-9e67-bb63-836e8e6ba3d2"), null },
                    { new Guid("c854d92c-53f0-62af-469a-54367d0ac7bf"), 250m, 73, new Guid("14c8c000-da8f-ba14-3e54-52df0949ac66"), null },
                    { new Guid("d9fe224f-0c5b-3653-adcf-e097be8b1085"), 50m, 0, new Guid("85efb04f-05d5-2318-3bbb-1149a9593e99"), 1 },
                    { new Guid("e1a3888e-a1ce-926b-2ccb-0118f80da5e1"), 150m, 3, new Guid("7e645de7-824d-4876-2e7e-d433d76e1f92"), 5 }
                });

            migrationBuilder.InsertData(
                table: "WarehouseChangeBatchItems",
                columns: new[] { "Id", "BatchId", "BillOfLadingId", "BlNumber", "ContainerNumber", "ErrorCode", "ErrorMessage", "FromWarehouse", "LineNumber", "ProcessedAt", "Status", "TariffCode", "ToWarehouse", "WarehouseChangeId" },
                values: new object[] { new Guid("9e413a2f-a1b6-9608-e41a-10e3ceebf9f8"), new Guid("99999999-0020-0020-0020-000000000001"), new Guid("11111111-0007-0007-0007-000000000010"), "HLCUSAI260401020", null, null, null, null, 1, new DateTime(2026, 10, 3, 9, 0, 6, 0, DateTimeKind.Utc), "Succeeded", null, "Bodega Central Santiago", new Guid("99999999-000f-000f-000f-000000000004") });

            migrationBuilder.InsertData(
                table: "ServiceRequestCharges",
                columns: new[] { "Id", "Generated", "LocalChargeId", "ServiceRequestId" },
                values: new object[,]
                {
                    { new Guid("52e277a6-8cb1-cc63-5049-2fedff932df2"), true, new Guid("33333333-0009-0009-0009-000000000028"), new Guid("ffffffff-0021-0021-0021-000000000003") },
                    { new Guid("8ae9ad55-dc43-4036-1737-82dd6d6dbb1a"), true, new Guid("33333333-0009-0009-0009-000000000029"), new Guid("ffffffff-0021-0021-0021-000000000004") },
                    { new Guid("b1d45b30-156b-9dba-dc89-95fce3304f08"), true, new Guid("33333333-0009-0009-0009-000000000027"), new Guid("ffffffff-0021-0021-0021-000000000002") }
                });

            migrationBuilder.InsertData(
                table: "ServiceRequestEvents",
                columns: new[] { "Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus" },
                values: new object[,]
                {
                    { new Guid("0c46ef99-c9b2-b2da-4dd1-2a64950e2c88"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Draft", null, new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000002"), "Submitted" },
                    { new Guid("0eca138e-a6fe-d34f-d7ce-0b313d25b1e8"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, new DateTime(2026, 10, 3, 9, 54, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000005"), "Draft" },
                    { new Guid("12312f19-f1e7-a19b-087d-db4cedffac15"), "System", "SYSTEM", null, "Submitted", "Total 14280 CLP.", new DateTime(2026, 10, 5, 15, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000002"), "PendingPayment" },
                    { new Guid("1cdf9dfc-60fa-4097-b2aa-4f5f6182dafc"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Draft", "Se reprogramó el ingreso de la unidad.", new DateTime(2026, 10, 4, 11, 20, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000006"), "Cancelled" },
                    { new Guid("1ceb83e1-e697-1a75-3ae8-511c892693dc"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, new DateTime(2026, 10, 5, 14, 57, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000002"), "Draft" },
                    { new Guid("1fe56423-ce52-019c-88b2-a2e4c87dde8d"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Draft", null, new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000001"), "Submitted" },
                    { new Guid("22d850c0-4f1f-76d9-e0f8-59b4fa87dd8e"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Draft", null, new DateTime(2026, 10, 2, 12, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000004"), "Submitted" },
                    { new Guid("233e7636-4120-570f-a96d-992cd1d85e44"), "System", "SYSTEM", null, "Paid", null, new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), 5, new Guid("ffffffff-0021-0021-0021-000000000004"), "InProgress" },
                    { new Guid("2949375f-ff9f-e06a-b38a-69272b88ab4d"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, new DateTime(2026, 10, 4, 11, 0, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000006"), "Draft" },
                    { new Guid("2cb530f0-29e1-b3a6-14d0-43149ea77b16"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Draft", null, new DateTime(2026, 10, 3, 10, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000005"), "Submitted" },
                    { new Guid("3cadc7b7-cb67-e64b-d863-ef36787e6e4c"), "System", "PAYMENT PAY-20261002-6F5E4D3C", null, "PendingPayment", "Comprobante RCP-20261002-7A8B9C0D.", new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), 4, new Guid("ffffffff-0021-0021-0021-000000000004"), "Paid" },
                    { new Guid("46f143ca-839c-ab02-0a69-f2027e5795c0"), "System", "SYSTEM", null, "Submitted", "Sin cobro: SOC:HLXU5566779.", new DateTime(2026, 10, 4, 15, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000007"), "Completed" },
                    { new Guid("4c71ac76-58f3-656e-b492-6cdcf494a2a4"), "System", "SYSTEM", null, "Paid", null, new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), 5, new Guid("ffffffff-0021-0021-0021-000000000003"), "InProgress" },
                    { new Guid("4d4b11bb-2ce8-5b4b-c2b5-89809ec47b3c"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, new DateTime(2026, 10, 5, 18, 0, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000008"), "Draft" },
                    { new Guid("8bc9ea6d-6047-ed03-8715-865e0038dbcd"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), "Draft", null, new DateTime(2026, 10, 1, 13, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000003"), "Submitted" },
                    { new Guid("8e7eaa46-1c0c-6d0e-b3ae-bb3d92296391"), "System", "PAYMENT PAY-20261002-6F5E4D3C", null, "PendingPayment", "Comprobante RCP-20261002-7A8B9C0D.", new DateTime(2026, 10, 2, 15, 0, 0, 0, DateTimeKind.Utc), 4, new Guid("ffffffff-0021-0021-0021-000000000003"), "Paid" },
                    { new Guid("9fd48303-f1f1-ab06-8ee0-8d0b60faba6a"), "Internal", "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), "PendingApproval", "El depósito Quilicura no recibe unidades 40HC esta semana; solicite la devolución en Pudahuel.", new DateTime(2026, 10, 3, 16, 0, 0, 0, DateTimeKind.Utc), 4, new Guid("ffffffff-0021-0021-0021-000000000005"), "Rejected" },
                    { new Guid("ad14be4a-ebbe-be59-6e22-a876f398fb72"), "Client", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), "Draft", null, new DateTime(2026, 10, 4, 15, 0, 0, 0, DateTimeKind.Utc), 2, new Guid("ffffffff-0021-0021-0021-000000000007"), "Submitted" },
                    { new Guid("ba24d9bc-4ee8-8753-b4b4-0b6aebd0d574"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, new DateTime(2026, 10, 5, 13, 55, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000001"), "Draft" },
                    { new Guid("c6a6da09-505c-a4d8-78f9-700398403b24"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, new DateTime(2026, 10, 1, 12, 50, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000003"), "Draft" },
                    { new Guid("cca46ba1-145e-2be1-d0e0-8af2dd5a0e97"), "System", "SYSTEM", null, "Submitted", "Total 53550 CLP.", new DateTime(2026, 10, 1, 13, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000003"), "PendingPayment" },
                    { new Guid("dec466f0-24bf-71ac-7ec9-bcc8f5a9a315"), "System", "SYSTEM", null, "Submitted", "Derivada al equipo ED.", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000001"), "PendingApproval" },
                    { new Guid("e49d521c-ba5c-2e4e-666a-136461165ef3"), "System", "SYSTEM", null, "Submitted", "Derivada al equipo ED.", new DateTime(2026, 10, 3, 10, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000005"), "PendingApproval" },
                    { new Guid("e67dd7d2-1b76-a227-fe0d-cd0a4904e01c"), "Client", "demo@importadorademo.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000010"), null, null, new DateTime(2026, 10, 2, 11, 56, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000004"), "Draft" },
                    { new Guid("ed4f28fa-e309-d42f-6b2b-ef4b2c1db0c0"), "Internal", "admin@hapag-lloyd.cl", new Guid("d4e5f6a7-0004-0004-0004-000000000001"), "InProgress", "BL hijo transmitido y aceptado por Aduana.", new DateTime(2026, 10, 3, 10, 0, 0, 0, DateTimeKind.Utc), 6, new Guid("ffffffff-0021-0021-0021-000000000004"), "Completed" },
                    { new Guid("edbd4f6a-c3e0-4180-4fc6-24cb71a0e524"), "Client", "demo@altiplano.bo", new Guid("d4e5f6a7-0004-0004-0004-000000000020"), null, null, new DateTime(2026, 10, 4, 14, 58, 0, 0, DateTimeKind.Utc), 1, new Guid("ffffffff-0021-0021-0021-000000000007"), "Draft" },
                    { new Guid("f20e8fcb-2085-f157-43aa-eb5751bdf13a"), "System", "SYSTEM", null, "Submitted", "Total 120 USD.", new DateTime(2026, 10, 2, 12, 0, 0, 0, DateTimeKind.Utc), 3, new Guid("ffffffff-0021-0021-0021-000000000004"), "PendingPayment" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDefinitions_Code",
                table: "ServiceDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDefinitions_IsActive_DisplayOrder",
                table: "ServiceDefinitions",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestAttachments_ServiceRequestId_FieldKey",
                table: "ServiceRequestAttachments",
                columns: new[] { "ServiceRequestId", "FieldKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestCharges_LocalChargeId",
                table: "ServiceRequestCharges",
                column: "LocalChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestCharges_ServiceRequestId_LocalChargeId",
                table: "ServiceRequestCharges",
                columns: new[] { "ServiceRequestId", "LocalChargeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestEvents_ServiceRequestId_OccurredAt_Sequence",
                table: "ServiceRequestEvents",
                columns: new[] { "ServiceRequestId", "OccurredAt", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_BillOfLadingId_DefinitionId",
                table: "ServiceRequests",
                columns: new[] { "BillOfLadingId", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_DefinitionId",
                table: "ServiceRequests",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_OrganizationId_CreatedAt",
                table: "ServiceRequests",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_RequestNumber",
                table: "ServiceRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Status_AssignedTeam",
                table: "ServiceRequests",
                columns: new[] { "Status", "AssignedTeam" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceRequestAttachments");

            migrationBuilder.DropTable(
                name: "ServiceRequestCharges");

            migrationBuilder.DropTable(
                name: "ServiceRequestEvents");

            migrationBuilder.DropTable(
                name: "ServiceRequests");

            migrationBuilder.DropTable(
                name: "ServiceDefinitions");

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000019"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000020"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000021"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000022"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("04860673-2222-59b2-d04c-6a971962465b"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("14a10063-2e24-3dad-0f58-d91f07276e71"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("2ba2ee40-e9b4-20e6-5f43-3597e9b1e3e4"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("349644e9-cc16-f243-5929-b308f01814d7"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("7cb784b5-5363-d794-f9b9-ba72edd30a69"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("95cfe440-ad17-116d-e382-d8d4925a8450"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("da68ecd8-b854-1daa-fea9-3c7e06e83e30"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("f27c3d60-1e67-07f7-b8f6-204b0f483bd5"));

            migrationBuilder.DeleteData(
                table: "ChargeConcepts",
                keyColumn: "Id",
                keyValue: new Guid("fc9e7715-ccf9-3b6a-4c06-744aea94758d"));

            migrationBuilder.DeleteData(
                table: "ExchangeRateRecords",
                keyColumn: "Id",
                keyValue: new Guid("ff504a00-3987-a4bb-a953-ee9fd8f4765c"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000027"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000028"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000029"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("0c84def6-4017-1cc8-5cb3-bc6949b15e84"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("0fb183cc-89fb-a728-538a-341c8a1ba5cc"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("25242d34-dba2-fbd8-3cc7-a482b769d040"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("2db0b575-0067-0cba-ac87-53cb2963e9e5"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("42237288-ed5e-c85a-2efc-d27408e207f2"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("431c7c6a-11ee-5094-96c8-8fc4cf1857e1"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("46f9d240-dab8-66bd-d19d-aa9e21346450"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("495338b3-b5bd-0ece-e4f1-2a53619a8bb1"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("4e72a36d-56b5-10c6-c568-24c15dd2a17e"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("534111a5-d9fe-9b27-3302-c3faf5ac50c8"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("5b857144-a78e-010d-dee9-a7cd0dd2cbf4"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("619584fa-1d71-2fc2-9b1b-74cab244ee62"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("70c99437-5242-1670-7f8d-cc32be458aef"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("76574e01-7eec-b1c6-fd96-d86cae36ffac"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("7ddf473a-2e3b-af3d-0a5c-e2f912d005cf"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("84c4c7c9-2237-1f1c-fc66-3d44058448b1"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("959121c1-5b6a-a81b-f74c-33d4af71595b"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("a1fbcd01-ec97-6f0b-e0dd-da2db2f77f15"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("a6924dd1-4d90-21a1-5a01-037e1f908345"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("c5544fec-42c2-acc4-7ada-c458f513372e"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("d880e37a-b4e7-4968-d22c-7722998ad7d2"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("e2b446b6-0654-1934-f54a-a38e2ebd598a"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("e7efc002-82db-179e-b1f1-ab3f8d91e501"));

            migrationBuilder.DeleteData(
                table: "MaintainerChangeLogs",
                keyColumn: "Id",
                keyValue: new Guid("e8703b3e-1773-0ee3-ef72-9f591e2ae550"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("2d66e7bf-d1f8-ee1d-5010-79747e017882"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("30ada185-078c-2fbf-3a02-2812eb08a5a0"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("a832ba20-d7cd-be95-bd38-7c02c2b76c51"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("4b5dd8b9-21c3-ce5e-3bba-5c3c17ec632f"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("9bfbb3d4-412d-6600-9482-ef7f1e64220f"));

            migrationBuilder.DeleteData(
                table: "PaymentStatusChanges",
                keyColumn: "Id",
                keyValue: new Guid("a0b9ed1f-e4ac-fe81-28be-77676bf235aa"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("61285041-197b-46ea-7ea3-57f2c557eba3"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("075936e7-41be-53b0-1290-d3970873bf0f"));

            migrationBuilder.DeleteData(
                table: "ShipmentRoles",
                keyColumn: "Id",
                keyValue: new Guid("ef31b036-2222-2f88-9a2f-85178328013c"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("1115e1cb-e5e3-af7b-ce32-0b3cd2bf6644"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("36e03912-6741-6aaa-b1df-6030fc077295"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("3a54e555-290b-7035-3a46-d48779e9e782"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("59813e09-2340-3be1-6176-a8c729aad23d"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("5e0f0096-0f79-5f68-c614-61a6cf3cb064"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("63dcf8d2-9418-c776-3498-1f4c7ea28c6e"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("7922f54d-8324-e8a7-61ff-16a15e0f6ccc"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("82204082-2cb6-314d-5720-7d64f6f6d063"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("86950eae-1c4a-4602-f867-0be816597eea"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("8fb701ca-e302-16aa-bb3f-9d088b224703"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("9989fc8b-7c73-ac18-3ddc-456af0138636"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("a606e47d-f958-2e06-61cb-5669ff5e2329"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("c854d92c-53f0-62af-469a-54367d0ac7bf"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("d9fe224f-0c5b-3653-adcf-e097be8b1085"));

            migrationBuilder.DeleteData(
                table: "TariffTiers",
                keyColumn: "Id",
                keyValue: new Guid("e1a3888e-a1ce-926b-2ccb-0118f80da5e1"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("08f6a148-a021-357f-11e0-9e263e3bc70e"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("09121ab9-02ec-4885-dda1-1cc481c6ddd3"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("3f963e93-b130-ae94-6ee6-8426587d9197"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("4f0226d1-eabb-3e9f-6355-41589b2ceb4a"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("8f9cd335-f4f6-df32-36b6-94794d2906dc"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("ea3a5a01-c4ee-76ca-9d51-f9e542075cc7"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("ed9325ae-cb8d-6b70-cfc6-3bfaeaff51e2"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("f383edf7-a9c4-fff5-2c56-78c53ce50bc1"));

            migrationBuilder.DeleteData(
                table: "WarehouseChangeBatchItems",
                keyColumn: "Id",
                keyValue: new Guid("9e413a2f-a1b6-9608-e41a-10e3ceebf9f8"));

            migrationBuilder.DeleteData(
                table: "WarehouseChanges",
                keyColumn: "Id",
                keyValue: new Guid("99999999-000f-000f-000f-000000000003"));

            migrationBuilder.DeleteData(
                table: "WarehouseChanges",
                keyColumn: "Id",
                keyValue: new Guid("99999999-000f-000f-000f-000000000004"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000016"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000017"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000014"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("14c8c000-da8f-ba14-3e54-52df0949ac66"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("7e645de7-824d-4876-2e7e-d433d76e1f92"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("85efb04f-05d5-2318-3bbb-1149a9593e99"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("b023bef2-0e70-2988-f28c-2da684ab098e"));

            migrationBuilder.DeleteData(
                table: "Tariffs",
                keyColumn: "Id",
                keyValue: new Guid("f69d95b8-610f-9e67-bb63-836e8e6ba3d2"));

            migrationBuilder.DeleteData(
                table: "WarehouseChangeBatches",
                keyColumn: "Id",
                keyValue: new Guid("99999999-0020-0020-0020-000000000001"));
        }
    }
}
