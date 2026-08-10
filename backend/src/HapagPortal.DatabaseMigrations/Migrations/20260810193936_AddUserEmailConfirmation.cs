using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000001"),
                columns: new[] { "EmailConfirmationTokenExpiry", "IsEmailConfirmed" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000010"),
                columns: new[] { "EmailConfirmationTokenExpiry", "IsEmailConfirmed" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000020"),
                columns: new[] { "EmailConfirmationTokenExpiry", "IsEmailConfirmed" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-0004-0004-0004-000000000030"),
                columns: new[] { "EmailConfirmationTokenExpiry", "IsEmailConfirmed" },
                values: new object[] { null, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailConfirmed",
                table: "Users");
        }
    }
}
