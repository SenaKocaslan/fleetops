using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FleetOps.Api.Auth.Migrations
{
    /// <inheritdoc />
    public partial class IlkKullanicilar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "app_user",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "auth",
                table: "app_user",
                columns: new[] { "id", "password_hash", "role", "user_name" },
                values: new object[,]
                {
                    { new Guid("dddddddd-0000-0000-0000-000000000001"), "100000.u1dXkDE+WT5SEH2bOdEpZg==.fLwykOL+TgFQ7akkw/MlEPmcBiWkUBi/kxrCkh2WUg8=", "Operator", "operator" },
                    { new Guid("dddddddd-0000-0000-0000-000000000002"), "100000.M5ugf+wFXLnNWojnB4Ok/Q==.dqXIJoJIK5JlCvxEKScj6oTIEkDUkArvkzqkICwGZAQ=", "Supervisor", "supervisor" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_user_user_name",
                schema: "auth",
                table: "app_user",
                column: "user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_user",
                schema: "auth");
        }
    }
}
