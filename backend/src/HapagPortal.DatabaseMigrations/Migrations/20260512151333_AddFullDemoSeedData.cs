using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFullDemoSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000001"),
                column: "NotifyParty",
                value: "Agencia Marítima del Pacífico Ltda");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000002"),
                column: "PortOfDischarge",
                value: "Valparaiso (CLVAP)");

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000010"),
                columns: new[] { "Address", "City" },
                values: new object[] { "Av. Providencia 1234, Of. 501", "Santiago" });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Address", "AgentCode", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "ModifiedAt", "ModifiedBy", "Name", "Phone", "TaxId", "TaxIdType" },
                values: new object[,]
                {
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "Calle Comercio 456", null, "La Paz", "Client", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "demo@altiplano.bo", true, true, null, null, "Comercial Altiplano SRL", "+591 2 211 5678", "1023456017", "NIT" },
                    { new Guid("c3d4e5f6-0003-0003-0003-000000000030"), "Blanco 1199, Of. 301", "AGT-CL-001", "Valparaíso", "Agent", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "agente@maritimpacifico.cl", true, true, null, null, "Agencia Marítima del Pacífico Ltda", "+56 32 225 1000", "96.555.444-3", "RUT" }
                });

            migrationBuilder.InsertData(
                table: "CreditClients",
                columns: new[] { "Id", "ApprovedAt", "ApprovedBy", "ClientId", "Country", "CreatedAt", "CreatedBy", "CreditLimit", "CreditStatus", "DeletedAt", "DeletedBy", "ExpiresAt", "ModifiedAt", "ModifiedBy" },
                values: new object[] { new Guid("77777777-000d-000d-000d-000000000002"), null, null, new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", 15000000m, "PendingApproval", null, null, null, null, null });

            migrationBuilder.InsertData(
                table: "DemurrageExemptions",
                columns: new[] { "Id", "ClientName", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "ModifiedAt", "ModifiedBy", "Reason", "TaxId" },
                values: new object[,]
                {
                    { new Guid("88888888-000e-000e-000e-000000000001"), "Agencia Marítima del Pacífico Ltda", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Acuerdo comercial preferencial — cliente con volumen superior a 500 TEU/año", "96.555.444-3" },
                    { new Guid("88888888-000e-000e-000e-000000000002"), "Comercial Altiplano SRL", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Exención por carga en tránsito internacional — convenio bilateral CL-BO", "1023456017" }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "ModifiedAt", "ModifiedBy", "PaymentDate", "PaymentMethod", "PaymentNumber", "PaymentType", "ReceiptNumber", "Status", "TaxAmount", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("55555555-000b-000b-000b-000000000001"), 210000m, new Guid("11111111-0007-0007-0007-000000000003"), new Guid("c3d4e5f6-0003-0003-0003-000000000001"), new DateTime(2026, 2, 20, 14, 31, 0, 0, DateTimeKind.Utc), "WEBPAY_AUTO", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, "WBP-TXN-20260220-001", null, null, new DateTime(2026, 2, 20, 14, 30, 0, 0, DateTimeKind.Utc), "WebPay", "PAY-2026-00001", "LocalCharges", "REC-2026-00001", "Confirmed", 39900m, 249900m },
                    { new Guid("55555555-000b-000b-000b-000000000002"), 230000m, new Guid("11111111-0007-0007-0007-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "/uploads/deposit-proof-002.pdf", null, null, null, null, new DateTime(2026, 4, 10, 10, 0, 0, 0, DateTimeKind.Utc), "BankTransfer", "PAY-2026-00002", "LocalCharges", null, "Pending", 43700m, 273700m }
                });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CompletedAt", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "ModifiedAt", "ModifiedBy", "OrderNumber", "OrderType", "RequestedAt", "Status" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0010-0010-0010-000000000001"), new Guid("11111111-0007-0007-0007-000000000003"), new Guid("c3d4e5f6-0003-0003-0003-000000000001"), new DateTime(2026, 2, 17, 15, 0, 0, 0, DateTimeKind.Utc), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Inspección fitosanitaria contenedor HLXU4455667 — maquinaria industrial procedente de Europa", null, null, "SO-2026-00001", "Inspection", new DateTime(2026, 2, 16, 8, 0, 0, 0, DateTimeKind.Utc), "Completed" },
                    { new Guid("aaaaaaaa-0010-0010-0010-000000000002"), new Guid("11111111-0007-0007-0007-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Solicitud de retiro contenedores HLXU1234567 y HLXU7654321 — electrónicos importados", null, null, "SO-2026-00002", "WarehouseRelease", new DateTime(2026, 4, 8, 10, 0, 0, 0, DateTimeKind.Utc), "InProgress" }
                });

            migrationBuilder.InsertData(
                table: "WarehouseChanges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "FromWarehouse", "ModifiedAt", "ModifiedBy", "Status", "ToWarehouse" },
                values: new object[] { new Guid("99999999-000f-000f-000f-000000000001"), 120000m, new Guid("11111111-0007-0007-0007-000000000001"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "STI San Antonio - Patio A", null, null, "Approved", "Bodega Central Santiago" });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "ModifiedAt", "ModifiedBy", "NotifyParty", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000004"), "HLCUARI260100045", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "Comercial Altiplano SRL", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 3, 28, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), 2800m, "USD", null, null, null, "Arica (CLARI)", "Ningbo (CNNGB)", "Import", "Ningbo Textiles Export Co.", "Arrived", "Antofagasta Express", "012E" },
                    { new Guid("11111111-0007-0007-0007-000000000005"), "HLCUIQQ260200078", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "Comercial Altiplano SRL", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1950m, "USD", null, null, null, "Iquique (CLIQQ)", "Mumbai (INBOM)", "Import", "Mumbai Spices & Commodities Pvt Ltd", "InTransit", "Guayaquil Express", "007W" }
                });

            migrationBuilder.InsertData(
                table: "CreditClients",
                columns: new[] { "Id", "ApprovedAt", "ApprovedBy", "ClientId", "Country", "CreatedAt", "CreatedBy", "CreditLimit", "CreditStatus", "DeletedAt", "DeletedBy", "ExpiresAt", "ModifiedAt", "ModifiedBy" },
                values: new object[] { new Guid("77777777-000d-000d-000d-000000000001"), new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("c3d4e5f6-0003-0003-0003-000000000030"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", 50000000m, "Approved", null, null, new DateTime(2027, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.InsertData(
                table: "PaymentDetails",
                columns: new[] { "Id", "Amount", "ConceptType", "Currency", "Description", "PaymentId", "TaxAmount" },
                values: new object[,]
                {
                    { new Guid("66666666-000c-000c-000c-000000000001"), 210000m, "THC", "CLP", "Terminal Handling Charge - 40OT", new Guid("55555555-000b-000b-000b-000000000001"), 39900m },
                    { new Guid("66666666-000c-000c-000c-000000000002"), 185000m, "THC", "CLP", "Terminal Handling Charge - 40HC", new Guid("55555555-000b-000b-000b-000000000002"), 35150m },
                    { new Guid("66666666-000c-000c-000c-000000000003"), 45000m, "BL_FEE", "CLP", "BL Documentation Fee", new Guid("55555555-000b-000b-000b-000000000002"), 8550m }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "LastLoginAt", "ModifiedAt", "ModifiedBy", "PasswordHash", "UserType", "Username" },
                values: new object[,]
                {
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000020"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "demo@altiplano.bo", true, null, null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", "Client", "demo@altiplano.bo" },
                    { new Guid("d4e5f6a7-0004-0004-0004-000000000030"), new Guid("c3d4e5f6-0003-0003-0003-000000000030"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "agente@maritimpacifico.cl", true, null, null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", "Agent", "agente@maritimpacifico.cl" }
                });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "SealNumber", "Status", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000006"), new Guid("11111111-0007-0007-0007-000000000004"), "HLXU8899001", "20DV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-011234", "Discharged", 15600m },
                    { new Guid("22222222-0008-0008-0008-000000000007"), new Guid("11111111-0007-0007-0007-000000000005"), "HLXU5566778", "40HC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-015678", "OnBoard", 21300m }
                });

            migrationBuilder.InsertData(
                table: "DemurrageCharges",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "CreatedAt", "CreatedBy", "Currency", "DailyRate", "DeletedAt", "DeletedBy", "DemurrageDays", "EndDate", "ExemptReason", "FreeDays", "IsExempt", "ModifiedAt", "ModifiedBy", "StartDate", "Status", "TotalAmount" },
                values: new object[] { new Guid("44444444-000a-000a-000a-000000000003"), new Guid("11111111-0007-0007-0007-000000000004"), "HLXU8899001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", 310m, null, null, 8, new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, false, null, null, new DateTime(2026, 3, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Pending", 2480m });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("33333333-0009-0009-0009-000000000007"), 1280m, new Guid("11111111-0007-0007-0007-000000000004"), "THC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Terminal Handling Charge - 20DV (Arica)", true, null, null, "Pending", 166.40m, 13m, 1446.40m },
                    { new Guid("33333333-0009-0009-0009-000000000008"), 690m, new Guid("11111111-0007-0007-0007-000000000004"), "TRANSIT_FEE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "Bolivia Transit Documentation Fee", true, null, null, "Pending", 89.70m, 13m, 779.70m }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "ModifiedAt", "ModifiedBy", "PaymentDate", "PaymentMethod", "PaymentNumber", "PaymentType", "ReceiptNumber", "Status", "TaxAmount", "TotalAmount" },
                values: new object[] { new Guid("55555555-000b-000b-000b-000000000003"), 1970m, new Guid("11111111-0007-0007-0007-000000000004"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "/uploads/deposit-proof-003.pdf", 6.91m, null, null, null, new DateTime(2026, 4, 2, 9, 0, 0, 0, DateTimeKind.Utc), "BankDeposit", "PAY-2026-00003", "LocalCharges", null, "Pending", 256.10m, 2226.10m });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "BillOfLadingId", "ClientId", "CompletedAt", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "ModifiedAt", "ModifiedBy", "OrderNumber", "OrderType", "RequestedAt", "Status" },
                values: new object[] { new Guid("aaaaaaaa-0010-0010-0010-000000000003"), new Guid("11111111-0007-0007-0007-000000000004"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Documentación de tránsito internacional Arica → La Paz para contenedor HLXU8899001", null, null, "SO-2026-00003", "TransitDocumentation", new DateTime(2026, 4, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Pending" });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "RoleName", "UserId" },
                values: new object[,]
                {
                    { new Guid("e5f6a7b8-0005-0005-0005-000000000020"), "User", new Guid("d4e5f6a7-0004-0004-0004-000000000020") },
                    { new Guid("e5f6a7b8-0005-0005-0005-000000000030"), "User", new Guid("d4e5f6a7-0004-0004-0004-000000000030") }
                });

            migrationBuilder.InsertData(
                table: "WarehouseChanges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "FromWarehouse", "ModifiedAt", "ModifiedBy", "Status", "ToWarehouse" },
                values: new object[] { new Guid("99999999-000f-000f-000f-000000000002"), 850m, new Guid("11111111-0007-0007-0007-000000000004"), "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, "TPA Arica - Zona Franca", null, null, "Pending", "Almacén Aduana La Paz" });

            migrationBuilder.InsertData(
                table: "PaymentDetails",
                columns: new[] { "Id", "Amount", "ConceptType", "Currency", "Description", "PaymentId", "TaxAmount" },
                values: new object[,]
                {
                    { new Guid("66666666-000c-000c-000c-000000000004"), 1280m, "THC", "BOB", "Terminal Handling Charge - 20DV (Arica)", new Guid("55555555-000b-000b-000b-000000000003"), 166.40m },
                    { new Guid("66666666-000c-000c-000c-000000000005"), 690m, "TRANSIT_FEE", "BOB", "Bolivia Transit Documentation Fee", new Guid("55555555-000b-000b-000b-000000000003"), 89.70m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0011-0011-0011-000000000001"));

            migrationBuilder.DeleteData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0011-0011-0011-000000000002"));

            migrationBuilder.DeleteData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0011-0011-0011-000000000003"));

            migrationBuilder.DeleteData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0011-0011-0011-000000000004"));

            migrationBuilder.DeleteData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0011-0011-0011-000000000005"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000006"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000007"));

            migrationBuilder.DeleteData(
                table: "CreditClients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-000d-000d-000d-000000000001"));

            migrationBuilder.DeleteData(
                table: "CreditClients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-000d-000d-000d-000000000002"));

            migrationBuilder.DeleteData(
                table: "DemurrageCharges",
                keyColumn: "Id",
                keyValue: new Guid("44444444-000a-000a-000a-000000000003"));

            migrationBuilder.DeleteData(
                table: "DemurrageExemptions",
                keyColumn: "Id",
                keyValue: new Guid("88888888-000e-000e-000e-000000000001"));

            migrationBuilder.DeleteData(
                table: "DemurrageExemptions",
                keyColumn: "Id",
                keyValue: new Guid("88888888-000e-000e-000e-000000000002"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000007"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000008"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000001"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000002"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000003"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000004"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000005"));

            migrationBuilder.DeleteData(
                table: "ServiceOrders",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0010-0010-0010-000000000001"));

            migrationBuilder.DeleteData(
                table: "ServiceOrders",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0010-0010-0010-000000000002"));

            migrationBuilder.DeleteData(
                table: "ServiceOrders",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0010-0010-0010-000000000003"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000020"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000030"));

            migrationBuilder.DeleteData(
                table: "WarehouseChanges",
                keyColumn: "Id",
                keyValue: new Guid("99999999-000f-000f-000f-000000000001"));

            migrationBuilder.DeleteData(
                table: "WarehouseChanges",
                keyColumn: "Id",
                keyValue: new Guid("99999999-000f-000f-000f-000000000002"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000005"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000001"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000002"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000003"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000020"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000030"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000004"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000030"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000020"));

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000001"),
                column: "NotifyParty",
                value: null);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000002"),
                column: "PortOfDischarge",
                value: "Valparaíso (CLVAP)");

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000010"),
                columns: new[] { "Address", "City" },
                values: new object[] { null, null });
        }
    }
}
