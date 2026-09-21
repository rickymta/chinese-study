using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Cms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W3b_Faqs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "faqs",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    answer_markdown = table.Column<string>(type: "text", nullable: false),
                    group_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "general"),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faqs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_faqs_published_group_sort",
                schema: "site",
                table: "faqs",
                columns: new[] { "is_published", "group_key", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faqs",
                schema: "site");
        }
    }
}
