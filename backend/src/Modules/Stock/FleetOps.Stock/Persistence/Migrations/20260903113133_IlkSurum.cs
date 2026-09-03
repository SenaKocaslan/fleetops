using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FleetOps.Stock.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IlkSurum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stock");

            migrationBuilder.CreateTable(
                name: "location",
                schema: "stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    zone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_integration_event",
                schema: "stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_integration_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement",
                schema: "stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    from_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    moved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movement", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movement_location_from_location_id",
                        column: x => x.from_location_id,
                        principalSchema: "stock",
                        principalTable: "location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movement_location_to_location_id",
                        column: x => x.to_location_id,
                        principalSchema: "stock",
                        principalTable: "location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "stock",
                table: "location",
                columns: new[] { "id", "code", "zone" },
                values: new object[,]
                {
                    { new Guid("cccccccc-0000-0000-0000-000000000001"), "KABUL-01", "Kabul" },
                    { new Guid("cccccccc-0000-0000-0000-000000000002"), "RAF-A1", "Depo" },
                    { new Guid("cccccccc-0000-0000-0000-000000000003"), "RAF-B2", "Depo" },
                    { new Guid("cccccccc-0000-0000-0000-000000000004"), "SEVK-01", "Sevkiyat" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_location_code",
                schema: "stock",
                table: "location",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_from_location_id",
                schema: "stock",
                table: "stock_movement",
                column: "from_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_moved_at_utc",
                schema: "stock",
                table: "stock_movement",
                column: "moved_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_to_location_id",
                schema: "stock",
                table: "stock_movement",
                column: "to_location_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_integration_event",
                schema: "stock");

            migrationBuilder.DropTable(
                name: "stock_movement",
                schema: "stock");

            migrationBuilder.DropTable(
                name: "location",
                schema: "stock");
        }
    }
}
