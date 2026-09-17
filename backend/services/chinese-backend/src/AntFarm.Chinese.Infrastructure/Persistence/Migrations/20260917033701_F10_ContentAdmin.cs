using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Chinese.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F10_ContentAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "content",
                table: "words",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "ix_words_review",
                schema: "content",
                table: "words",
                columns: new[] { "meaning_vi_status", "hsk3_level", "path_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_words_review",
                schema: "content",
                table: "words");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "content",
                table: "words");
        }
    }
}
