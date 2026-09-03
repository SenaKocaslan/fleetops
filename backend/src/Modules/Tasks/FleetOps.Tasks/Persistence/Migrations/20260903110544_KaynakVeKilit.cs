using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FleetOps.Tasks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KaynakVeKilit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resource",
                schema: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_lock",
                schema: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agv_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acquired_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    released_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_lock", x => x.id);
                    table.ForeignKey(
                        name: "fk_resource_lock_resource_resource_id",
                        column: x => x.resource_id,
                        principalSchema: "tasks",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "tasks",
                table: "resource",
                columns: new[] { "id", "code", "kind" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "DOCK-1", "ChargingDock" },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000002"), "CORRIDOR-A", "Corridor" },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000003"), "LIFT-1", "Lift" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_resource_code",
                schema: "tasks",
                table: "resource",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_resource_lock_aktif_kaynak",
                schema: "tasks",
                table: "resource_lock",
                column: "resource_id",
                unique: true,
                filter: "released_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_resource_lock_released_at_utc_expires_at_utc",
                schema: "tasks",
                table: "resource_lock",
                columns: new[] { "released_at_utc", "expires_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resource_lock",
                schema: "tasks");

            migrationBuilder.DropTable(
                name: "resource",
                schema: "tasks");
        }
    }
}
