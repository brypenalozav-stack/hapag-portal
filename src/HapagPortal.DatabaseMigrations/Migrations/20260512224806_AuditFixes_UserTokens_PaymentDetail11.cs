using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AuditFixes_UserTokens_PaymentDetail11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.InsertData(
                table: "PaymentDetails",
                columns: new[] { "Id", "Amount", "ConceptType", "Currency", "Description", "PaymentId", "TaxAmount" },
                values: new object[] { new Guid("66666666-000c-000c-000c-000000000011"), 1750000m, "Freight", "CLP", "Flete marítimo BL HLCUVAL250300789", new Guid("55555555-000b-000b-000b-000000000008"), 332500m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000001"),
                columns: new[] { "EmailConfirmationToken", "PasswordResetToken", "PasswordResetTokenExpiry" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000010"),
                columns: new[] { "EmailConfirmationToken", "PasswordResetToken", "PasswordResetTokenExpiry" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000020"),
                columns: new[] { "EmailConfirmationToken", "PasswordResetToken", "PasswordResetTokenExpiry" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000030"),
                columns: new[] { "EmailConfirmationToken", "PasswordResetToken", "PasswordResetTokenExpiry" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PaymentDetails",
                keyColumn: "Id",
                keyValue: new Guid("66666666-000c-000c-000c-000000000011"));

            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "Users");
        }
    }
}
