using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M1_MobileClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_app",
                schema: "identity",
                table: "refresh_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_type",
                schema: "identity",
                table: "refresh_tokens",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "web");

            migrationBuilder.AddColumn<string>(
                name: "device_name",
                schema: "identity",
                table: "refresh_tokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_refresh_tokens_client_type",
                schema: "identity",
                table: "refresh_tokens",
                sql: "client_type IN ('web', 'mobile')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_refresh_tokens_client_type",
                schema: "identity",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "client_app",
                schema: "identity",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "client_type",
                schema: "identity",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "device_name",
                schema: "identity",
                table: "refresh_tokens");
        }
    }
}
