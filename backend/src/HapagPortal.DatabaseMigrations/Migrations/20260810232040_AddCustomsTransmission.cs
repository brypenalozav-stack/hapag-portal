using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomsTransmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomsManifests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VesselImo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Voyage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Port = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EstimatedArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedDeparture = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomsManifests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomsTransmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManifestId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillOfLadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomsTransmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomsTransmissions_BillsOfLading_BillOfLadingId",
                        column: x => x.BillOfLadingId,
                        principalTable: "BillsOfLading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomsTransmissions_CustomsManifests_ManifestId",
                        column: x => x.ManifestId,
                        principalTable: "CustomsManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomsTransmissionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ResponseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomsTransmissionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomsTransmissionEvents_CustomsTransmissions_Transmission~",
                        column: x => x.TransmissionId,
                        principalTable: "CustomsTransmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomsManifests_VesselImo_Voyage_Direction",
                table: "CustomsManifests",
                columns: new[] { "VesselImo", "Voyage", "Direction" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomsTransmissionEvents_TransmissionId",
                table: "CustomsTransmissionEvents",
                column: "TransmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomsTransmissions_BillOfLadingId",
                table: "CustomsTransmissions",
                column: "BillOfLadingId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomsTransmissions_ManifestId",
                table: "CustomsTransmissions",
                column: "ManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomsTransmissions_Status",
                table: "CustomsTransmissions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomsTransmissionEvents");

            migrationBuilder.DropTable(
                name: "CustomsTransmissions");

            migrationBuilder.DropTable(
                name: "CustomsManifests");
        }
    }
}
