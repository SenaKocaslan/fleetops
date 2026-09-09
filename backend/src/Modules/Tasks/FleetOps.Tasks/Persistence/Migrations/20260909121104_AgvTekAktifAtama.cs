using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetOps.Tasks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgvTekAktifAtama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_assignment_agv_id_completed_at_utc",
                schema: "tasks",
                table: "task_assignment");

            migrationBuilder.CreateIndex(
                name: "ix_task_assignment_agv_id",
                schema: "tasks",
                table: "task_assignment",
                column: "agv_id",
                unique: true,
                filter: "completed_at_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_assignment_agv_id",
                schema: "tasks",
                table: "task_assignment");

            migrationBuilder.CreateIndex(
                name: "ix_task_assignment_agv_id_completed_at_utc",
                schema: "tasks",
                table: "task_assignment",
                columns: new[] { "agv_id", "completed_at_utc" });
        }
    }
}
