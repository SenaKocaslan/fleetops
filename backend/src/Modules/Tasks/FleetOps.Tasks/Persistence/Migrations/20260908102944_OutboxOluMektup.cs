using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetOps.Tasks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxOluMektup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_message_processed_at_utc_occurred_at_utc",
                schema: "tasks",
                table: "outbox_message");

            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                schema: "tasks",
                table: "outbox_message",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "dead_lettered_at_utc",
                schema: "tasks",
                table: "outbox_message",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_at_utc_dead_lettered_at_utc_occurr",
                schema: "tasks",
                table: "outbox_message",
                columns: new[] { "processed_at_utc", "dead_lettered_at_utc", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_message_processed_at_utc_dead_lettered_at_utc_occurr",
                schema: "tasks",
                table: "outbox_message");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                schema: "tasks",
                table: "outbox_message");

            migrationBuilder.DropColumn(
                name: "dead_lettered_at_utc",
                schema: "tasks",
                table: "outbox_message");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_at_utc_occurred_at_utc",
                schema: "tasks",
                table: "outbox_message",
                columns: new[] { "processed_at_utc", "occurred_at_utc" });
        }
    }
}
