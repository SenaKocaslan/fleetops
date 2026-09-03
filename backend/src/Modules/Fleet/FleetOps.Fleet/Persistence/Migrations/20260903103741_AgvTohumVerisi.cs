using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FleetOps.Fleet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgvTohumVerisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "fleet",
                table: "agv",
                columns: new[] { "id", "battery_level", "code", "current_location_id", "status" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 95, "AGV-01", null, "Available" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 60, "AGV-02", null, "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 12, "AGV-03", null, "Charging" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "fleet",
                table: "agv",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                schema: "fleet",
                table: "agv",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                schema: "fleet",
                table: "agv",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}
