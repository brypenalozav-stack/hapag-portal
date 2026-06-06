using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class FixDtoAlignmentAndExpandSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CreditClients",
                columns: new[] { "Id", "ApprovedAt", "ApprovedBy", "ClientId", "Country", "CreatedAt", "CreatedBy", "CreditLimit", "CreditStatus", "DeletedAt", "DeletedBy", "ExpiresAt", "ModifiedAt", "ModifiedBy" },
                values: new object[,]
                {
                    { new Guid("77777777-000d-000d-000d-000000000003"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("c3d4e5f6-0003-0003-0003-000000000020"), "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", 350000m, "Approved", null, null, new DateTime(2027, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null },
                    { new Guid("77777777-000d-000d-000d-000000000004"), new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", new Guid("c3d4e5f6-0003-0003-0003-000000000001"), "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", 100000000m, "Suspended", null, null, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, null }
                });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000003"),
                column: "PaymentMethod",
                value: "Khipu");

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "BillOfLadingId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "ModifiedAt", "ModifiedBy", "PaymentDate", "PaymentMethod", "PaymentNumber", "PaymentType", "ReceiptNumber", "Status", "TaxAmount", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("55555555-000b-000b-000b-000000000004"), 2850000m, new Guid("11111111-0007-0007-0007-000000000001"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), new DateTime(2026, 3, 15, 11, 2, 0, 0, DateTimeKind.Utc), "GATEWAY_AUTO", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, "CC-TXN-20260315-004", null, null, new DateTime(2026, 3, 15, 11, 0, 0, 0, DateTimeKind.Utc), "CreditCard", "PAY-2026-00004", "Freight", "REC-2026-00004", "Confirmed", 541500m, 3391500m },
                    { new Guid("55555555-000b-000b-000b-000000000005"), 330000m, new Guid("11111111-0007-0007-0007-000000000002"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, "WBP-TXN-20260405-FAIL", null, null, new DateTime(2026, 4, 5, 16, 0, 0, 0, DateTimeKind.Utc), "WebPay", "PAY-2026-00005", "Demurrage", null, "Failed", 62700m, 392700m },
                    { new Guid("55555555-000b-000b-000b-000000000006"), 4500m, new Guid("11111111-0007-0007-0007-000000000005"), new Guid("c3d4e5f6-0003-0003-0003-000000000020"), null, null, "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "BOB", null, null, null, 6.91m, null, null, null, new DateTime(2026, 3, 28, 8, 0, 0, 0, DateTimeKind.Utc), "BankTransfer", "PAY-2026-00006", "Freight", null, "Cancelled", 585m, 5085m },
                    { new Guid("55555555-000b-000b-000b-000000000007"), 480000m, new Guid("11111111-0007-0007-0007-000000000002"), new Guid("c3d4e5f6-0003-0003-0003-000000000010"), null, null, "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, "WBP-TXN-20260412-007", null, null, new DateTime(2026, 4, 12, 9, 30, 0, 0, DateTimeKind.Utc), "WebPay", "PAY-2026-00007", "LocalCharges", null, "Processing", 91200m, 571200m },
                    { new Guid("55555555-000b-000b-000b-000000000008"), 1750000m, new Guid("11111111-0007-0007-0007-000000000003"), new Guid("c3d4e5f6-0003-0003-0003-000000000030"), new DateTime(2026, 2, 19, 10, 0, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl", "CL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", "CLP", null, null, null, null, null, null, null, new DateTime(2026, 2, 18, 15, 0, 0, 0, DateTimeKind.Utc), "BankTransfer", "PAY-2026-00008", "Freight", "REC-2026-00008", "Confirmed", 332500m, 2082500m }
                });

            migrationBuilder.InsertData(
                table: "PaymentDetails",
                columns: new[] { "Id", "Amount", "ConceptType", "Currency", "Description", "PaymentId", "TaxAmount" },
                values: new object[,]
                {
                    { new Guid("66666666-000c-000c-000c-000000000006"), 2850000m, "Freight", "CLP", "Ocean Freight - Shanghai to Valparaiso", new Guid("55555555-000b-000b-000b-000000000004"), 541500m },
                    { new Guid("66666666-000c-000c-000c-000000000007"), 330000m, "Demurrage", "CLP", "Demurrage charges - 8 days", new Guid("55555555-000b-000b-000b-000000000005"), 62700m },
                    { new Guid("66666666-000c-000c-000c-000000000008"), 4500m, "Freight", "BOB", "Ocean Freight - Santos to Arica", new Guid("55555555-000b-000b-000b-000000000006"), 585m },
                    { new Guid("66666666-000c-000c-000c-000000000009"), 185000m, "THC", "CLP", "Terminal Handling Charge - 20DV", new Guid("55555555-000b-000b-000b-000000000007"), 35150m },
                    { new Guid("66666666-000c-000c-000c-000000000010"), 295000m, "THC_RF", "CLP", "Reefer THC Surcharge", new Guid("55555555-000b-000b-000b-000000000007"), 56050m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CreditClients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-000d-000d-000d-000000000003"));

            migrationBuilder.DeleteData(
                table: "CreditClients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-000d-000d-000d-000000000004"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000006"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000007"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000008"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000009"));

            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000010"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000008"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000004"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000005"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000006"));

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000007"));

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000003"),
                column: "PaymentMethod",
                value: "BankDeposit");
        }
    }
}
