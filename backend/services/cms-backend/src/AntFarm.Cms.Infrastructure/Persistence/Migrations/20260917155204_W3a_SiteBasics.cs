using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Cms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W3a_SiteBasics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "site");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    native_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    tagline = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, defaultValue: ""),
                    description_markdown = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    app_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    accent_color = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    cover_media_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_languages", x => x.id);
                    table.CheckConstraint("ck_languages_status", "status IN ('open','coming_soon','hidden')");
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "site",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_at_desc",
                schema: "site",
                table: "audit_logs",
                column: "at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_target",
                schema: "site",
                table: "audit_logs",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_languages_sort_order",
                schema: "site",
                table: "languages",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ux_languages_code",
                schema: "site",
                table: "languages",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "site");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "site");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "site");
        }
    }
}
