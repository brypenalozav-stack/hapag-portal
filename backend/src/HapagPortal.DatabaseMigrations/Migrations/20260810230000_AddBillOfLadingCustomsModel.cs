using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddBillOfLadingCustomsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GrossWeight",
                table: "BLContainers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsShipperOwned",
                table: "BLContainers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IsoTypeCode",
                table: "BLContainers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageCount",
                table: "BLContainers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Tare",
                table: "BLContainers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Vgm",
                table: "BLContainers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BLType",
                table: "BillsOfLading",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FreightTerms",
                table: "BillsOfLading",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Incoterm",
                table: "BillsOfLading",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSeaWaybill",
                table: "BillsOfLading",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsToOrder",
                table: "BillsOfLading",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OperatorVoyage",
                table: "BillsOfLading",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentBLId",
                table: "BillsOfLading",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VesselImo",
                table: "BillsOfLading",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BLCargoItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    HsCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PackageCount = table.Column<int>(type: "integer", nullable: true),
                    PackageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    GrossWeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    NetWeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    Volume = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    ShippingMarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BLCargoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BLCargoItems_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BLParties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TaxIdType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BLParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BLParties_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000001"),
                columns: new[] { "GrossWeight", "IsShipperOwned", "IsoTypeCode", "PackageCount", "Tare", "Vgm" },
                values: new object[] { null, false, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000002"),
                columns: new[] { "GrossWeight", "IsShipperOwned", "IsoTypeCode", "PackageCount", "Tare", "Vgm" },
                values: new object[] { null, false, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000003"),
                columns: new[] { "GrossWeight", "IsShipperOwned", "IsoTypeCode", "PackageCount", "Tare", "Vgm" },
                values: new object[] { null, false, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000004"),
                columns: new[] { "GrossWeight", "IsShipperOwned", "IsoTypeCode", "PackageCount", "Tare", "Vgm" },
                values: new object[] { null, false, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000005"),
                columns: new[] { "GrossWeight", "IsShipperOwned", "IsoTypeCode", "PackageCount", "Tare", "Vgm" },
                values: new object[] { null, false, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000006"),
                columns: new[] { "GrossWeight", "IsShipperOwned", "IsoTypeCode", "PackageCount", "Tare", "Vgm" },
                values: new object[] { null, false, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "BLContainers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000007"),
                columns: new[] { "GrossWeight", "IsShipperOwned", "IsoTypeCode", "PackageCount", "Tare", "Vgm" },
                values: new object[] { null, false, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000001"),
                columns: new[] { "BLType", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "OperatorVoyage", "ParentBLId", "VesselImo" },
                values: new object[] { null, null, null, false, false, null, null, null });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000002"),
                columns: new[] { "BLType", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "OperatorVoyage", "ParentBLId", "VesselImo" },
                values: new object[] { null, null, null, false, false, null, null, null });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000003"),
                columns: new[] { "BLType", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "OperatorVoyage", "ParentBLId", "VesselImo" },
                values: new object[] { null, null, null, false, false, null, null, null });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000004"),
                columns: new[] { "BLType", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "OperatorVoyage", "ParentBLId", "VesselImo" },
                values: new object[] { null, null, null, false, false, null, null, null });

            migrationBuilder.UpdateData(
                table: "BillsOfLading",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000005"),
                columns: new[] { "BLType", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "OperatorVoyage", "ParentBLId", "VesselImo" },
                values: new object[] { null, null, null, false, false, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_BillsOfLading_ParentBLId",
                table: "BillsOfLading",
                column: "ParentBLId");

            migrationBuilder.CreateIndex(
                name: "IX_BLCargoItems_BillOfLadingId",
                table: "BLCargoItems",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_BLParties_BillOfLadingId",
                table: "BLParties",
                column: "BillOfLadingId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillsOfLading_BillsOfLading_ParentBLId",
                table: "BillsOfLading",
                column: "ParentBLId",
                principalTable: "BillsOfLading",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillsOfLading_BillsOfLading_ParentBLId",
                table: "BillsOfLading");

            migrationBuilder.DropTable(
                name: "BLCargoItems");

            migrationBuilder.DropTable(
                name: "BLParties");

            migrationBuilder.DropIndex(
                name: "IX_BillsOfLading_ParentBLId",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "GrossWeight",
                table: "BLContainers");

            migrationBuilder.DropColumn(
                name: "IsShipperOwned",
                table: "BLContainers");

            migrationBuilder.DropColumn(
                name: "IsoTypeCode",
                table: "BLContainers");

            migrationBuilder.DropColumn(
                name: "PackageCount",
                table: "BLContainers");

            migrationBuilder.DropColumn(
                name: "Tare",
                table: "BLContainers");

            migrationBuilder.DropColumn(
                name: "Vgm",
                table: "BLContainers");

            migrationBuilder.DropColumn(
                name: "BLType",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "FreightTerms",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "Incoterm",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "IsSeaWaybill",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "IsToOrder",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "OperatorVoyage",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "ParentBLId",
                table: "BillsOfLading");

            migrationBuilder.DropColumn(
                name: "VesselImo",
                table: "BillsOfLading");
        }
    }
}
