using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadlineRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeadlineRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseEvent = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OffsetHours = table.Column<int>(type: "integer", nullable: false),
                    AtRiskWindowHours = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    BLType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Certainty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_DeadlineRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeadlineInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManifestId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    BaseEventAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeadlineInstances_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineInstances_CustomsManifests_ManifestId",
                        column: x => x.ManifestId,
                        principalTable: "CustomsManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeadlineInstances_DeadlineRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "DeadlineRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DeadlineRules",
                columns: new[] { "Id", "AtRiskWindowHours", "BLType", "BaseEvent", "Certainty", "Code", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Direction", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "OffsetHours", "Severity", "Source" },
                values: new object[,]
                {
                    { new Guid("3f8af1ca-8cc6-3021-231a-ccfd3fcb29e1"), 6, "House", "ArrivalEstimated", "ToVerify", "BL_HOUSE_IN", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Ingreso", true, null, null, "B/L Hijo (ingreso)", -24, "High", "Material oficial Aduana" },
                    { new Guid("4044ddb6-21da-c608-87bd-3dd6589224c7"), 6, null, "DepartureEstimated", "Confirmed", "GOODS_DELIVERY", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, true, null, null, "Entrega de mercancías a almacenista", 24, "Medium", "Cap. 3 CNA num. 2.4" },
                    { new Guid("5baf30ac-36f3-d799-478d-54101caa3166"), 12, "Master", "ArrivalEstimated", "ToVerify", "BL_MASTER_IN", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Ingreso", true, null, null, "B/L Máster (ingreso)", -48, "High", "Material oficial Aduana (verificar Res. 7591/2012)" },
                    { new Guid("5bbd7a0d-7499-a627-e68c-347732ed175d"), 12, null, "DepartureEstimated", "Confirmed", "MANIFEST_HEADER_OUT", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Salida", true, null, null, "Encabezado manifiesto (salida)", -48, "High", "Res. 9432/2008" },
                    { new Guid("6bacf231-3e61-31b0-04d3-14c9a09fa33e"), 12, null, "DespatchRequest", "Confirmed", "MICDTA_BO", "BO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, null, true, null, null, "MIC/DTA tránsito (Bolivia)", 48, "Medium", "Cap. 3 CNA" },
                    { new Guid("75e384a7-55d8-941b-81a3-95b7411539ee"), 48, null, "DepartureEstimated", "Confirmed", "MANIFEST_AMEND_IN", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Ingreso", true, null, null, "Aclaración al manifiesto (ingreso)", 168, "Medium", "Cap. 3 CNA num. 2.6" },
                    { new Guid("aa5613e1-c370-afc4-f954-962e4f6cd1d8"), 48, null, "ArrivalEstimated", "Confirmed", "MANIFEST_HEADER_IN", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Ingreso", true, null, null, "Encabezado manifiesto (ingreso)", -168, "High", "Ficha aduana.cl" },
                    { new Guid("cd12e932-04f7-6f7c-b694-a28bb2eac93a"), 24, null, "DepartureEstimated", "Uncertain", "BL_EMPTY_OUT", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Salida", true, null, null, "B/L y contenedores vacíos (salida)", 72, "Low", "Res. 6609/2012 (valor por confirmar)" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineInstances_BillOfLadingId",
                table: "DeadlineInstances",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineInstances_ManifestId",
                table: "DeadlineInstances",
                column: "ManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineInstances_RuleId_ManifestId_BillOfLadingId",
                table: "DeadlineInstances",
                columns: new[] { "RuleId", "ManifestId", "BillOfLadingId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineRules_Code",
                table: "DeadlineRules",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeadlineInstances");

            migrationBuilder.DropTable(
                name: "DeadlineRules");
        }
    }
}
