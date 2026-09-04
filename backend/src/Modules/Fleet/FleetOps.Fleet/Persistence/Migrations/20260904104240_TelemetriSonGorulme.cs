using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetOps.Fleet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TelemetriSonGorulme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_seen_at_utc",
                schema: "fleet",
                table: "agv",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "fleet",
                table: "agv",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "last_seen_at_utc",
                value: null);

            migrationBuilder.UpdateData(
                schema: "fleet",
                table: "agv",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "last_seen_at_utc",
                value: null);

            migrationBuilder.UpdateData(
                schema: "fleet",
                table: "agv",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "last_seen_at_utc",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_seen_at_utc",
                schema: "fleet",
                table: "agv");
        }
    }
}
