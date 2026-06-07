using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TaxIdType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AgentCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ExchangeRateToUSD = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemurrageExemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_DemurrageExemptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FAQs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_FAQs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ServiceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TaxName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillsOfLading",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BLNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShipmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Vessel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Voyage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PortOfLoading = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PortOfDischarge = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PlaceOfDelivery = table.Column<string>(type: "text", nullable: true),
                    ETD = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ETA = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consignee = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Shipper = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotifyParty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FreightAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FreightCurrency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillsOfLading", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillsOfLading_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditClients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditClients_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UserType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "text", nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailConfirmationToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BLContainers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContainerType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SealNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BLContainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BLContainers_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemurrageCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FreeDays = table.Column<int>(type: "integer", nullable: false),
                    DemurrageDays = table.Column<int>(type: "integer", nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsExempt = table.Column<bool>(type: "boolean", nullable: false),
                    ExemptReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemurrageCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemurrageCharges_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsTaxable = table.Column<bool>(type: "boolean", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalCharges_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceiptNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DepositProofUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrders_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceOrders_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromWarehouse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ToWarehouse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseChanges_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConceptType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentDetails_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AuditLogs",
                columns: new[] { "Id", "Action", "EntityId", "EntityName", "NewValues", "OldValues", "Timestamp", "UserId" },
                values: new object[,]
                {
                    { new Guid("bbbbbbbb-0011-0011-0011-000000000001"), "Created", "55555555-000b-000b-000b-000000000001", "Payment", "{\"PaymentNumber\":\"PAY-2026-00001\",\"Status\":\"Pending\"}", null, new DateTime(2026, 2, 20, 14, 30, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000001" },
                    { new Guid("bbbbbbbb-0011-0011-0011-000000000002"), "Updated", "55555555-000b-000b-000b-000000000001", "Payment", "{\"Status\":\"Confirmed\"}", "{\"Status\":\"Pending\"}", new DateTime(2026, 2, 20, 14, 31, 0, 0, DateTimeKind.Utc), "WEBPAY_AUTO" },
                    { new Guid("bbbbbbbb-0011-0011-0011-000000000003"), "Created", "aaaaaaaa-0010-0010-0010-000000000001", "ServiceOrder", "{\"OrderNumber\":\"SO-2026-00001\",\"OrderType\":\"Inspection\"}", null, new DateTime(2026, 2, 16, 8, 0, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000001" },
                    { new Guid("bbbbbbbb-0011-0011-0011-000000000004"), "Created", "99999999-000f-000f-000f-000000000001", "WarehouseChange", "{\"FromWarehouse\":\"STI San Antonio - Patio A\",\"ToWarehouse\":\"Bodega Central Santiago\"}", null, new DateTime(2026, 4, 6, 11, 0, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000010" },
                    { new Guid("bbbbbbbb-0011-0011-0011-000000000005"), "Updated", "77777777-000d-000d-000d-000000000001", "CreditClient", "{\"CreditStatus\":\"Approved\"}", "{\"CreditStatus\":\"PendingApproval\"}", new DateTime(2026, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), "d4e5f6a7-0004-0004-0004-000000000001" }
                });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Address", "AgentCode", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "ModifiedAt", "ModifiedBy", "Name", "Phone", "TaxId", "TaxIdType" },
                values: new object[,]
                {
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000001"), null, null, null, "Internal", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "admin@hapag-lloyd.cl", true, true, null, null, "Hapag-Lloyd Administrador", "+56 2 2630 1700", "99999999-K", "RUT" },
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Av. Providencia 1234, Of. 501", null, "Santiago", "Client", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "demo@importadorademo.cl", true, true, null, null, "Importadora Demo SpA", "+56 2 2345 6789", "76.123.456-7", "RUT" },
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "Calle Comercio 456", null, "La Paz", "Client", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "demo@altiplano.bo", true, true, null, null, "Comercial Altiplano SRL", "+591 2 211 5678", "1023456017", "NIT" },
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000030"), "Blanco 1199, Of. 301", "AGT-CL-001", "Valparaíso", "Agent", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "agente@maritimpacifico.cl", true, true, null, null, "Agencia Marítima del Pacífico Ltda", "+56 32 225 1000", "96.555.444-3", "RUT" }
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ExchangeRateToUSD", "LastUpdated", "ModifiedAt", "ModifiedBy", "Name", "Symbol" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-0001-0001-000000000001"), "CLP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 950m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Peso Chileno", "$" },
                    { new Guid("a1b2c3d4-0001-0001-0001-000000000002"), "BOB", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 6.91m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Boliviano", "Bs" },
                    { new Guid("a1b2c3d4-0001-0001-0001-000000000003"), "USD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, 1m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "US Dollar", "$" }
                });

            migrationBuilder.InsertData(
                table: "DemurrageExemptions",
                columns: new[] { "Id", "ClientName", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "ModifiedAt", "ModifiedBy", "Reason", "TaxId" },
                values: new object[,]
                {
                    { new Guid("88888888-000e-000e-000e-000000000001"), "Agencia Marítima del Pacífico Ltda", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Acuerdo comercial preferencial — cliente con volumen superior a 500 TEU/año", "96.555.444-3" },
                    { new Guid("88888888-000e-000e-000e-000000000002"), "Comercial Altiplano SRL", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Exención por carga en tránsito internacional — convenio bilateral CL-BO", "1023456017" }
                });

            migrationBuilder.InsertData(
                table: "FAQs",
                columns: new[] { "Id", "Answer", "Category", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "ModifiedAt", "ModifiedBy", "Question", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000001"), "Ingrese al módulo 'Bills of Lading', escriba su número de BL en el buscador y presione buscar. Verá el detalle completo incluyendo contenedores, cargos locales y demurrage.", "SHIPPING", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cómo puedo consultar el estado de mi BL?", 1 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000002"), "En Chile puede pagar con Tarjeta de Crédito, Tarjeta de Débito, Transferencia Bancaria y WebPay. Todos los pagos electrónicos se procesan en tiempo real.", "PAYMENTS", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Qué métodos de pago están disponibles en Chile?", 2 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000003"), "Vaya al módulo 'Cambio de Almacén', ingrese el número de BL, el contenedor, el almacén actual y el almacén destino. La solicitud será procesada y recibirá confirmación por correo.", "SHIPPING", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cómo solicito un cambio de almacén?", 3 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000004"), "La tasa de IVA vigente en Chile es del 19%. Se aplica automáticamente sobre los cargos locales y servicios facturables.", "PAYMENTS", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cuál es la tasa de IVA aplicada en Chile?", 4 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000005"), "Una vez confirmado el pago, vaya al detalle del pago y presione 'Generar Recibo'. El recibo se genera automáticamente en formato PDF con todos los datos fiscales.", "DOCUMENTATION", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cómo genero un recibo de pago?", 5 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000011"), "Ingrese al módulo 'Bills of Lading' y busque por número de BL. Verá el estado de su carga incluyendo el puerto de ingreso (Arica, Iquique o Antofagasta) y los cargos asociados.", "SHIPPING", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cómo puedo consultar el estado de mi BL en Bolivia?", 1 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000012"), "En Bolivia puede pagar mediante Transferencia Bancaria, Efectivo y Cheque. Los pagos en efectivo deben realizarse en oficinas autorizadas.", "PAYMENTS", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Qué métodos de pago están disponibles en Bolivia?", 2 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000013"), "La tasa de IVA vigente en Bolivia es del 13%. Se aplica automáticamente sobre los cargos locales y servicios facturables. Los montos se manejan en Bolivianos (BOB).", "PAYMENTS", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cuál es la tasa de IVA aplicada en Bolivia?", 3 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000014"), "El NIT (Número de Identificación Tributaria) es el identificador fiscal en Bolivia. Es obligatorio para el registro en el portal y para la emisión de documentos fiscales.", "GENERAL", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Qué es el NIT y por qué lo necesito?", 4 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000015"), "El demurrage se calcula desde la fecha de descarga en el puerto chileno. Los días libres y tarifas diarias dependen del tipo de contenedor y acuerdos comerciales. Puede solicitar exenciones a través del módulo de Demurrage.", "DEMURRAGE", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cómo funciona el demurrage para carga en tránsito a Bolivia?", 5 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000021"), "Haga clic en 'Registrarse', seleccione su país (Chile o Bolivia), ingrese los datos de su empresa (RUT/NIT, nombre, correo) y cree una contraseña. Recibirá un correo de confirmación.", "GENERAL", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cómo registro mi empresa en el portal?", 10 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000022"), "En la pantalla de login, haga clic en '¿Olvidó su contraseña?'. Ingrese su correo electrónico y recibirá un enlace para restablecer su contraseña.", "GENERAL", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Olvidé mi contraseña, cómo la recupero?", 11 },
                    { new Guid("f6a7b8c9-0006-0006-0006-000000000023"), "Puede contactarnos al correo clservice@hapag-lloyd.com o llamar al +56 2 2630 1700 (Chile) / +591 2 211 0700 (Bolivia) en horario de oficina de lunes a viernes.", "GENERAL", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "¿Cómo contacto a soporte técnico?", 12 }
                });

            migrationBuilder.InsertData(
                table: "TaxConfigurations",
                columns: new[] { "Id", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "EffectiveFrom", "EffectiveTo", "IsActive", "ModifiedAt", "ModifiedBy", "ServiceType", "TaxName", "TaxRate" },
                values: new object[,]
                {
                    { new Guid("b2c3d4e5-0002-0002-0002-000000000001"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, "General", "IVA", 19m },
                    { new Guid("b2c3d4e5-0002-0002-0002-000000000002"), "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, "General", "IVA", 13m }
                });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "ModifiedAt", "ModifiedBy", "NotifyParty", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000001"), "HLCUVAL250100123", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3500m, "USD", null, null, "Agencia Marítima del Pacífico Ltda", "Santiago, Chile", "San Antonio (CLSAI)", "Shanghai (CNSHA)", "Import", "Shanghai Electronics Co. Ltd", "Arrived", "Hamburg Express", "025E" },
                    { new Guid("11111111-0007-0007-0007-000000000002"), "HLCUVAL250200456", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), 5200m, "USD", null, null, null, "Valparaiso, Chile", "Valparaiso (CLVAP)", "Busan (KRPUS)", "Import", "Korea Auto Parts Inc.", "InTransit", "Berlin Express", "031W" },
                    { new Guid("11111111-0007-0007-0007-000000000003"), "HLCUVAL250300789", new Guid("c3d4e5f6-0003-0003-0003-000000000001"), "Hapag-Lloyd Administrador", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), 8750m, "USD", null, null, null, "Santiago, Chile", "San Antonio (CLSAI)", "Rotterdam (NLRTM)", "Import", "European Machinery GmbH", "Delivered", "Colombo Express", "018E" },
                    { new Guid("11111111-0007-0007-0007-000000000004"), "HLCUARI260100045", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "Comercial Altiplano SRL", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 3, 28, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), 2800m, "USD", null, null, null, "La Paz, Bolivia", "Arica (CLARI)", "Ningbo (CNNGB)", "Import", "Ningbo Textiles Export Co.", "Arrived", "Antofagasta Express", "012E" },
                    { new Guid("11111111-0007-0007-0007-000000000005"), "HLCUIQQ260200078", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "Comercial Altiplano SRL", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1950m, "USD", null, null, null, "Santa Cruz, Bolivia", "Iquique (CLIQQ)", "Mumbai (INBOM)", "Import", "Mumbai Spices & Commodities Pvt Ltd", "InTransit", "Guayaquil Express", "007W" }
                });

            migrationBuilder.InsertData(
                table: "CreditClients",
                columns: new[] { "Id", "ApprovedAt", "ApprovedBy", "ClientId", "Country", "CreatedAt", "CreatedBy", "CreditLimit", "CreditStatus", "DeletedAt", "DeletedBy", "ExpiresAt", "ModifiedAt", "ModifiedBy" },
                values: new object[,]
                {
                    { new Guid("77777777-000d-000d-000d-000000000001"), new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("c3d4e5f6-0003-0003-0003-000000000030"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", 50000000m, "Approved", null, null, new DateTime(2027, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, null },
                    { new Guid("77777777-000d-000d-000d-000000000002"), null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", 15000000m, "PendingApproval", null, null, null, null, null },
                    { new Guid("77777777-000d-000d-000d-000000000003"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", 350000m, "Approved", null, null, new DateTime(2027, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null },
                    { new Guid("77777777-000d-000d-000d-000000000004"), new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("c3d4e5f6-0003-0003-0003-000000000001"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", 100000000m, "Suspended", null, null, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "EmailConfirmationToken", "IsActive", "LastLoginAt", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username" },
                values: new object[,]
                {
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000001"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "admin@hapag-lloyd.cl", null, true, null, null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, "Admin", "admin@hapag-lloyd.cl" },
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "demo@importadorademo.cl", null, true, null, null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, "Client", "demo@importadorademo.cl" },
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000020"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "demo@altiplano.bo", null, true, null, null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, "Client", "demo@altiplano.bo" },
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000030"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "agente@maritimpacifico.cl", null, true, null, null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", null, null, null, null, "Agent", "agente@maritimpacifico.cl" }
                });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "SealNumber", "Status", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000001"), new Guid("11111111-0007-0007-0007-000000000001"), "HLXU1234567", "40HC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-001234", "Discharged", 24500m },
                    { new Guid("22222222-0008-0008-0008-000000000002"), new Guid("11111111-0007-0007-0007-000000000001"), "HLXU7654321", "20DV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-005678", "Discharged", 18200m },
                    { new Guid("22222222-0008-0008-0008-000000000003"), new Guid("11111111-0007-0007-0007-000000000002"), "HLXU9876543", "40HC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-009012", "OnBoard", 22100m },
                    { new Guid("22222222-0008-0008-0008-000000000004"), new Guid("11111111-0007-0007-0007-000000000002"), "HLXU1112233", "40RF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-003456", "OnBoard", 19800m },
                    { new Guid("22222222-0008-0008-0008-000000000005"), new Guid("11111111-0007-0007-0007-000000000003"), "HLXU4455667", "40OT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-007890", "Delivered", 31500m },
                    { new Guid("22222222-0008-0008-0008-000000000006"), new Guid("11111111-0007-0007-0007-000000000004"), "HLXU8899001", "20DV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-011234", "Discharged", 15600m },
                    { new Guid("22222222-0008-0008-0008-000000000007"), new Guid("11111111-0007-0007-0007-000000000005"), "HLXU5566778", "40HC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-015678", "OnBoard", 21300m }
                });

            migrationBuilder.InsertData(
                table: "DemurrageCharges",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "CreatedAt", "CreatedBy", "Currency", "DailyRate", "DeletedAt", "DeletedBy", "DemurrageDays", "EndDate", "ExemptReason", "FreeDays", "IsExempt", "ModifiedAt", "ModifiedBy", "StartDate", "Status", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("44444444-000a-000a-000a-000000000001"), new Guid("11111111-0007-0007-0007-000000000001"), "HLXU1234567", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", 45000m, null, null, 5, new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, false, null, null, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Pending", 225000m },
                    { new Guid("44444444-000a-000a-000a-000000000002"), new Guid("11111111-0007-0007-0007-000000000001"), "HLXU7654321", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", 35000m, null, null, 3, new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, false, null, null, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Pending", 105000m },
                    { new Guid("44444444-000a-000a-000a-000000000003"), new Guid("11111111-0007-0007-0007-000000000004"), "HLXU8899001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", 310m, null, null, 8, new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, false, null, null, new DateTime(2026, 3, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Pending", 2480m }
                });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000001"), 185000m, new Guid("11111111-0007-0007-0007-000000000001"), "THC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 40HC", true, null, null, "Pending", 35150m, 19m, 220150m },
                    { new Guid("33333333-0009-0009-0009-000000000002"), 45000m, new Guid("11111111-0007-0007-0007-000000000001"), "BL_FEE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "BL Documentation Fee", true, null, null, "Pending", 8550m, 19m, 53550m },
                    { new Guid("33333333-0009-0009-0009-000000000003"), 25000m, new Guid("11111111-0007-0007-0007-000000000001"), "ISPS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "ISPS Security Surcharge", true, null, null, "Pending", 4750m, 19m, 29750m },
                    { new Guid("33333333-0009-0009-0009-000000000004"), 185000m, new Guid("11111111-0007-0007-0007-000000000002"), "THC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 40HC", true, null, null, "Pending", 35150m, 19m, 220150m },
                    { new Guid("33333333-0009-0009-0009-000000000005"), 295000m, new Guid("11111111-0007-0007-0007-000000000002"), "THC_RF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 40RF Reefer", true, null, null, "Pending", 56050m, 19m, 351050m },
                    { new Guid("33333333-0009-0009-0009-000000000006"), 210000m, new Guid("11111111-0007-0007-0007-000000000003"), "THC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 40OT", true, null, null, "Paid", 39900m, 19m, 249900m },
                    { new Guid("33333333-0009-0009-0009-000000000007"), 1280m, new Guid("11111111-0007-0007-0007-000000000004"), "THC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Terminal Handling Charge - 20DV (Arica)", true, null, null, "Pending", 166.40m, 13m, 1446.40m },
                    { new Guid("33333333-0009-0009-0009-000000000008"), 690m, new Guid("11111111-0007-0007-0007-000000000004"), "TRANSIT_FEE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Bolivia Transit Documentation Fee", true, null, null, "Pending", 89.70m, 13m, 779.70m }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "ModifiedAt", "ModifiedBy", "PaymentDate", "PaymentMethod", "PaymentNumber", "PaymentType", "ReceiptNumber", "Status", "TaxAmount", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("55555555-000b-000b-000b-000000000001"), 210000m, new Guid("11111111-0007-0007-0007-000000000003"), new Guid("c3d4e5f6-0003-0003-0003-000000000001"), new DateTime(2026, 2, 20, 14, 31, 0, 0, DateTimeKind.Utc), "WEBPAY_AUTO", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, "WBP-TXN-20260220-001", null, null, new DateTime(2026, 2, 20, 14, 30, 0, 0, DateTimeKind.Utc), "WebPay", "PAY-2026-00001", "LocalCharges", "REC-2026-00001", "Confirmed", 39900m, 249900m },
                    { new Guid("55555555-000b-000b-000b-000000000002"), 230000m, new Guid("11111111-0007-0007-0007-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "/uploads/deposit-proof-002.pdf", null, null, null, null, new DateTime(2026, 4, 10, 10, 0, 0, 0, DateTimeKind.Utc), "BankTransfer", "PAY-2026-00002", "LocalCharges", null, "Pending", 43700m, 273700m },
                    { new Guid("55555555-000b-000b-000b-000000000003"), 1970m, new Guid("11111111-0007-0007-0007-000000000004"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "/uploads/deposit-proof-003.pdf", 6.91m, null, null, null, new DateTime(2026, 4, 2, 9, 0, 0, 0, DateTimeKind.Utc), "Khipu", "PAY-2026-00003", "LocalCharges", null, "Pending", 256.10m, 2226.10m },
                    { new Guid("55555555-000b-000b-000b-000000000004"), 2850000m, new Guid("11111111-0007-0007-0007-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 3, 15, 11, 2, 0, 0, DateTimeKind.Utc), "GATEWAY_AUTO", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, "CC-TXN-20260315-004", null, null, new DateTime(2026, 3, 15, 11, 0, 0, 0, DateTimeKind.Utc), "CreditCard", "PAY-2026-00004", "Freight", "REC-2026-00004", "Confirmed", 541500m, 3391500m },
                    { new Guid("55555555-000b-000b-000b-000000000005"), 330000m, new Guid("11111111-0007-0007-0007-000000000002"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, "WBP-TXN-20260405-FAIL", null, null, new DateTime(2026, 4, 5, 16, 0, 0, 0, DateTimeKind.Utc), "WebPay", "PAY-2026-00005", "Demurrage", null, "Failed", 62700m, 392700m },
                    { new Guid("55555555-000b-000b-000b-000000000006"), 4500m, new Guid("11111111-0007-0007-0007-000000000005"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, null, 6.91m, null, null, null, new DateTime(2026, 3, 28, 8, 0, 0, 0, DateTimeKind.Utc), "BankTransfer", "PAY-2026-00006", "Freight", null, "Cancelled", 585m, 5085m },
                    { new Guid("55555555-000b-000b-000b-000000000007"), 480000m, new Guid("11111111-0007-0007-0007-000000000002"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, "WBP-TXN-20260412-007", null, null, new DateTime(2026, 4, 12, 9, 30, 0, 0, DateTimeKind.Utc), "WebPay", "PAY-2026-00007", "LocalCharges", null, "Processing", 91200m, 571200m },
                    { new Guid("55555555-000b-000b-000b-000000000008"), 1750000m, new Guid("11111111-0007-0007-0007-000000000003"), new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new DateTime(2026, 2, 19, 10, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, null, null, null, new DateTime(2026, 2, 18, 15, 0, 0, 0, DateTimeKind.Utc), "BankTransfer", "PAY-2026-00008", "Freight", "REC-2026-00008", "Confirmed", 332500m, 2082500m }
                });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CompletedAt", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "ModifiedAt", "ModifiedBy", "OrderNumber", "OrderType", "RequestedAt", "Status" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0010-0010-0010-000000000001"), new Guid("11111111-0007-0007-0007-000000000003"), new Guid("c3d4e5f6-0003-0003-0003-000000000001"), new DateTime(2026, 2, 17, 15, 0, 0, 0, DateTimeKind.Utc), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Inspección fitosanitaria contenedor HLXU4455667 — maquinaria industrial procedente de Europa", null, null, "SO-2026-00001", "Inspection", new DateTime(2026, 2, 16, 8, 0, 0, 0, DateTimeKind.Utc), "Completed" },
                    { new Guid("aaaaaaaa-0010-0010-0010-000000000002"), new Guid("11111111-0007-0007-0007-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Solicitud de retiro contenedores HLXU1234567 y HLXU7654321 — electrónicos importados", null, null, "SO-2026-00002", "WarehouseRelease", new DateTime(2026, 4, 8, 10, 0, 0, 0, DateTimeKind.Utc), "InProgress" },
                    { new Guid("aaaaaaaa-0010-0010-0010-000000000003"), new Guid("11111111-0007-0007-0007-000000000004"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Documentación de tránsito internacional Arica → La Paz para contenedor HLXU8899001", null, null, "SO-2026-00003", "TransitDocumentation", new DateTime(2026, 4, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Pending" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "RoleName", "UserId" },
                values: new object[,]
                {
                    { new Guid("e5f6a7b8-0005-0005-0005-000000000001"), "Admin", new Guid("d4e5f6a7-0004-0004-0004-000000000001") },
                    { new Guid("e5f6a7b8-0005-0005-0005-000000000010"), "User", new Guid("d4e5f6a7-0004-0004-0004-000000000010") },
                    { new Guid("e5f6a7b8-0005-0005-0005-000000000020"), "User", new Guid("d4e5f6a7-0004-0004-0004-000000000020") },
                    { new Guid("e5f6a7b8-0005-0005-0005-000000000030"), "User", new Guid("d4e5f6a7-0004-0004-0004-000000000030") }
                });

            migrationBuilder.InsertData(
                table: "WarehouseChanges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "FromWarehouse", "ModifiedAt", "ModifiedBy", "Status", "ToWarehouse" },
                values: new object[,]
                {
                    { new Guid("99999999-000f-000f-000f-000000000001"), 120000m, new Guid("11111111-0007-0007-0007-000000000001"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "STI San Antonio - Patio A", null, null, "Approved", "Bodega Central Santiago" },
                    { new Guid("99999999-000f-000f-000f-000000000002"), 850m, new Guid("11111111-0007-0007-0007-000000000004"), "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "TPA Arica - Zona Franca", null, null, "Pending", "Almacén Aduana La Paz" }
                });

            migrationBuilder.InsertData(
                table: "PaymentDetails",
                columns: new[] { "Id", "Amount", "ConceptType", "Currency", "Description", "PaymentId", "TaxAmount" },
                values: new object[,]
                {
                    { new Guid("66666666-000c-000c-000c-000000000001"), 210000m, "THC", "CLP", "Terminal Handling Charge - 40OT", new Guid("55555555-000b-000b-000b-000000000001"), 39900m },
                    { new Guid("66666666-000c-000c-000c-000000000002"), 185000m, "THC", "CLP", "Terminal Handling Charge - 40HC", new Guid("55555555-000b-000b-000b-000000000002"), 35150m },
                    { new Guid("66666666-000c-000c-000c-000000000003"), 45000m, "BL_FEE", "CLP", "BL Documentation Fee", new Guid("55555555-000b-000b-000b-000000000002"), 8550m },
                    { new Guid("66666666-000c-000c-000c-000000000004"), 1280m, "THC", "BOB", "Terminal Handling Charge - 20DV (Arica)", new Guid("55555555-000b-000b-000b-000000000003"), 166.40m },
                    { new Guid("66666666-000c-000c-000c-000000000005"), 690m, "TRANSIT_FEE", "BOB", "Bolivia Transit Documentation Fee", new Guid("55555555-000b-000b-000b-000000000003"), 89.70m },
                    { new Guid("66666666-000c-000c-000c-000000000006"), 2850000m, "Freight", "CLP", "Ocean Freight - Shanghai to Valparaiso", new Guid("55555555-000b-000b-000b-000000000004"), 541500m },
                    { new Guid("66666666-000c-000c-000c-000000000007"), 330000m, "Demurrage", "CLP", "Demurrage charges - 8 days", new Guid("55555555-000b-000b-000b-000000000005"), 62700m },
                    { new Guid("66666666-000c-000c-000c-000000000008"), 4500m, "Freight", "BOB", "Ocean Freight - Santos to Arica", new Guid("55555555-000b-000b-000b-000000000006"), 585m },
                    { new Guid("66666666-000c-000c-000c-000000000009"), 185000m, "THC", "CLP", "Terminal Handling Charge - 20DV", new Guid("55555555-000b-000b-000b-000000000007"), 35150m },
                    { new Guid("66666666-000c-000c-000c-000000000010"), 295000m, "THC_RF", "CLP", "Reefer THC Surcharge", new Guid("55555555-000b-000b-000b-000000000007"), 56050m },
                    { new Guid("66666666-000c-000c-000c-000000000011"), 1750000m, "Freight", "CLP", "Flete marítimo BL HLCUVAL250300789", new Guid("55555555-000b-000b-000b-000000000008"), 332500m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName",
                table: "AuditLogs",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BillsOfLading_BLNumber",
                table: "BillsOfLading",
                column: "BLNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillsOfLading_ClientId",
                table: "BillsOfLading",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BLContainers_BillOfLadingId",
                table: "BLContainers",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_BLContainers_ContainerNumber",
                table: "BLContainers",
                column: "ContainerNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Email",
                table: "Clients",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TaxId_Country",
                table: "Clients",
                columns: new[] { "TaxId", "Country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditClients_ClientId_Country",
                table: "CreditClients",
                columns: new[] { "ClientId", "Country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemurrageCharges_BillOfLadingId",
                table: "DemurrageCharges",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_DemurrageExemptions_TaxId_Country",
                table: "DemurrageExemptions",
                columns: new[] { "TaxId", "Country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FAQs_Country_Category",
                table: "FAQs",
                columns: new[] { "Country", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalCharges_BillOfLadingId",
                table: "LocalCharges",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDetails_PaymentId",
                table: "PaymentDetails",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BillOfLadingId",
                table: "Payments",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ClientId",
                table: "Payments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentNumber",
                table: "Payments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_BillOfLadingId",
                table: "ServiceOrders",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_ClientId",
                table: "ServiceOrders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_OrderNumber",
                table: "ServiceOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxConfigurations_Country_ServiceType",
                table: "TaxConfigurations",
                columns: new[] { "Country", "ServiceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleName",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClientId",
                table: "Users",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseChanges_BillOfLadingId",
                table: "WarehouseChanges",
                column: "BillOfLadingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BLContainers");

            migrationBuilder.DropTable(
                name: "CreditClients");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "DemurrageCharges");

            migrationBuilder.DropTable(
                name: "DemurrageExemptions");

            migrationBuilder.DropTable(
                name: "FAQs");

            migrationBuilder.DropTable(
                name: "LocalCharges");

            migrationBuilder.DropTable(
                name: "PaymentDetails");

            migrationBuilder.DropTable(
                name: "ServiceOrders");

            migrationBuilder.DropTable(
                name: "TaxConfigurations");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "WarehouseChanges");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "BillsOfLading");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
