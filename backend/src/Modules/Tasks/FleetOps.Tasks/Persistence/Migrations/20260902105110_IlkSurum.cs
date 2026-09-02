using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetOps.Tasks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IlkSurum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tasks");

            migrationBuilder.CreateTable(
                name: "transport_task",
                schema: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    from_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transport_task", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_assignment",
                schema: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agv_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_assignment", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_assignment_transport_task_task_id",
                        column: x => x.task_id,
                        principalSchema: "tasks",
                        principalTable: "transport_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_task_assignment_agv_id_completed_at_utc",
                schema: "tasks",
                table: "task_assignment",
                columns: new[] { "agv_id", "completed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_task_assignment_task_id",
                schema: "tasks",
                table: "task_assignment",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ix_transport_task_status_priority",
                schema: "tasks",
                table: "transport_task",
                columns: new[] { "status", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_assignment",
                schema: "tasks");

            migrationBuilder.DropTable(
                name: "transport_task",
                schema: "tasks");
        }
    }
}
