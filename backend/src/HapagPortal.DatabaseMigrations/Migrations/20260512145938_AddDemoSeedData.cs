using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "ModifiedAt", "ModifiedBy", "NotifyParty", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "Voyage" },
                values: new object[] { new Guid("11111111-0007-0007-0007-000000000003"), "HLCUVAL250300789", new Guid("c3d4e5f6-0003-0003-0003-000000000001"), "Hapag-Lloyd Administrador", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), 8750m, "USD", null, null, null, "San Antonio (CLSAI)", "Rotterdam (NLRTM)", "Import", "European Machinery GmbH", "Delivered", "Colombo Express", "018E" });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Address", "AgentCode", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "ModifiedAt", "ModifiedBy", "Name", "Phone", "TaxId", "TaxIdType" },
                values: new object[] { new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, null, "Client", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "demo@importadorademo.cl", true, true, null, null, "Importadora Demo SpA", "+56 2 2345 6789", "76.123.456-7", "RUT" });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "SealNumber", "Status", "Weight" },
                values: new object[] { new Guid("22222222-0008-0008-0008-000000000005"), new Guid("11111111-0007-0007-0007-000000000003"), "HLXU4455667", "40OT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-007890", "Delivered", 31500m });

            migrationBuilder.InsertData(
                table: "BillsOfLading",
                columns: new[] { "Id", "BLNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "ModifiedAt", "ModifiedBy", "NotifyParty", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "Voyage" },
                values: new object[,]
                {
                    { new Guid("11111111-0007-0007-0007-000000000001"), "HLCUVAL250100123", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3500m, "USD", null, null, null, "San Antonio (CLSAI)", "Shanghai (CNSHA)", "Import", "Shanghai Electronics Co. Ltd", "Arrived", "Hamburg Express", "025E" },
                    { new Guid("11111111-0007-0007-0007-000000000002"), "HLCUVAL250200456", new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "Importadora Demo SpA", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), 5200m, "USD", null, null, null, "Valparaíso (CLVAP)", "Busan (KRPUS)", "Import", "Korea Auto Parts Inc.", "InTransit", "Berlin Express", "031W" }
                });

            migrationBuilder.InsertData(
                table: "LocalCharges",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount" },
                values: new object[] { new Guid("33333333-0009-0009-0009-000000000006"), 210000m, new Guid("11111111-0007-0007-0007-000000000003"), "THC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 40OT", true, null, null, "Paid", 39900m, 19m, 249900m });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "LastLoginAt", "ModifiedAt", "ModifiedBy", "PasswordHash", "UserType", "Username" },
                values: new object[] { new Guid("d4e5f6a7-0004-0004-0004-000000000010"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "demo@importadorademo.cl", true, null, null, null, "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca", "Client", "demo@importadorademo.cl" });

            migrationBuilder.InsertData(
                table: "BLContainers",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "SealNumber", "Status", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-0008-0008-0008-000000000001"), new Guid("11111111-0007-0007-0007-000000000001"), "HLXU1234567", "40HC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-001234", "Discharged", 24500m },
                    { new Guid("22222222-0008-0008-0008-000000000002"), new Guid("11111111-0007-0007-0007-000000000001"), "HLXU7654321", "20DV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-005678", "Discharged", 18200m },
                    { new Guid("22222222-0008-0008-0008-000000000003"), new Guid("11111111-0007-0007-0007-000000000002"), "HLXU9876543", "40HC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-009012", "OnBoard", 22100m },
                    { new Guid("22222222-0008-0008-0008-000000000004"), new Guid("11111111-0007-0007-0007-000000000002"), "HLXU1112233", "40RF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, null, "SL-003456", "OnBoard", 19800m }
                });

            migrationBuilder.InsertData(
                table: "DemurrageCharges",
                columns: new[] { "Id", "BillOfLadingId", "ContainerNumber", "CreatedAt", "CreatedBy", "Currency", "DailyRate", "DeletedAt", "DeletedBy", "DemurrageDays", "EndDate", "ExemptReason", "FreeDays", "IsExempt", "ModifiedAt", "ModifiedBy", "StartDate", "Status", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("44444444-000a-000a-000a-000000000001"), new Guid("11111111-0007-0007-0007-000000000001"), "HLXU1234567", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", 45000m, null, null, 5, new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, false, null, null, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Pending", 225000m },
                    { new Guid("44444444-000a-000a-000a-000000000002"), new Guid("11111111-0007-0007-0007-000000000001"), "HLXU7654321", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", 35000m, null, null, 3, new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, false, null, null, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Pending", 105000m }
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
                    { new Guid("33333333-0009-0009-0009-000000000005"), 295000m, new Guid("11111111-0007-0007-0007-000000000002"), "THC_RF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, "Terminal Handling Charge - 40RF Reefer", true, null, null, "Pending", 56050m, 19m, 351050m }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "RoleName", "UserId" },
                values: new object[] { new Guid("e5f6a7b8-0005-0005-0005-000000000010"), "User", new Guid("d4e5f6a7-0004-0004-0004-000000000010") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000001"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000002"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000003"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000004"));

            migrationBuilder.DeleteData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000005"));

            migrationBuilder.DeleteData(
                table: "DemurrageCharges",
                keyColumn: "Id",
                keyValue: new Guid("44444444-000a-000a-000a-000000000001"));

            migrationBuilder.DeleteData(
                table: "DemurrageCharges",
                keyColumn: "Id",
                keyValue: new Guid("44444444-000a-000a-000a-000000000002"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000001"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000002"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000003"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000004"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000005"));

            migrationBuilder.DeleteData(
                table: "LocalCharges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0009-0009-0009-000000000006"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000010"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000001"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000002"));

            migrationBuilder.DeleteData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000003"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000010"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0003-0003-000000000010"));
        }
    }
}
