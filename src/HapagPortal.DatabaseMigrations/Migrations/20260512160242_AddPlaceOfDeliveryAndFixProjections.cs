using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceOfDeliveryAndFixProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaceOfDelivery",
                table: "BillsOfLading",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000001"),
                column: "PlaceOfDelivery",
                value: "Santiago, Chile");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000002"),
                column: "PlaceOfDelivery",
                value: "Valparaiso, Chile");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000003"),
                column: "PlaceOfDelivery",
                value: "Santiago, Chile");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000004"),
                column: "PlaceOfDelivery",
                value: "La Paz, Bolivia");

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000005"),
                column: "PlaceOfDelivery",
                value: "Santa Cruz, Bolivia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaceOfDelivery",
                table: "BillsOfLading");
        }
    }
}
