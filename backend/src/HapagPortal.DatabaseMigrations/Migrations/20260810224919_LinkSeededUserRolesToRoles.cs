using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class LinkSeededUserRolesToRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000001"),
                column: "RoleId",
                value: new Guid("b02bbf39-dfb0-6123-13fa-4ea772ff1dfb"));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000010"),
                column: "RoleId",
                value: new Guid("a99ecf29-a1a5-f563-a992-a59574d146c8"));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000020"),
                column: "RoleId",
                value: new Guid("a99ecf29-a1a5-f563-a992-a59574d146c8"));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000030"),
                column: "RoleId",
                value: new Guid("c2825776-5914-a3e3-0041-d2bface9e0b9"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000001"),
                column: "RoleId",
                value: null);

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000010"),
                column: "RoleId",
                value: null);

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000020"),
                column: "RoleId",
                value: null);

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("e5f6a7b8-0005-0005-0005-000000000030"),
                column: "RoleId",
                value: null);
        }
    }
}
