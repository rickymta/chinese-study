using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Chinese.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F6_Vocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "characters",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hanzi = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    traditional_variants = table.Column<List<string>>(type: "text[]", nullable: false),
                    pinyin_readings = table.Column<List<string>>(type: "text[]", nullable: false),
                    han_viet = table.Column<List<string>>(type: "text[]", nullable: false),
                    han_viet_by_pinyin = table.Column<string>(type: "jsonb", nullable: true),
                    han_viet_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    stroke_count = table.Column<short>(type: "smallint", nullable: true),
                    radical = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    radical_number = table.Column<short>(type: "smallint", nullable: true),
                    sources = table.Column<List<string>>(type: "text[]", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_characters", x => x.id);
                    table.CheckConstraint("ck_characters_han_viet_status", "han_viet_status IN ('derived','reviewed')");
                });

            migrationBuilder.CreateTable(
                name: "import_runs",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dataset = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    importer_version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    inserted = table.Column<int>(type: "integer", nullable: false),
                    updated = table.Column<int>(type: "integer", nullable: false),
                    unchanged = table.Column<int>(type: "integer", nullable: false),
                    invalid = table.Column<int>(type: "integer", nullable: false),
                    @protected = table.Column<int>(name: "protected", type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_runs", x => x.id);
                    table.CheckConstraint("ck_import_runs_status", "status IN ('succeeded','failed')");
                });

            migrationBuilder.CreateTable(
                name: "words",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    simplified = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    traditional = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    variants = table.Column<List<string>>(type: "text[]", nullable: false),
                    pinyin = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    pinyin_compact = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    pinyin_search = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    hsk3_level = table.Column<short>(type: "smallint", nullable: true),
                    hsk2_level = table.Column<short>(type: "smallint", nullable: true),
                    hsk_exam2026_level = table.Column<short>(type: "smallint", nullable: true),
                    official_index = table.Column<short>(type: "smallint", nullable: true),
                    path_order = table.Column<int>(type: "integer", nullable: true),
                    frequency_rank = table.Column<int>(type: "integer", nullable: true),
                    pos = table.Column<List<string>>(type: "text[]", nullable: false),
                    usage_note = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    meanings_en = table.Column<List<string>>(type: "text[]", nullable: false),
                    meanings_vi = table.Column<List<string>>(type: "text[]", nullable: false),
                    meaning_vi_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    meaning_vi_source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    han_viet = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    han_viet_plain = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    han_viet_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    search_vi = table.Column<string>(type: "text", nullable: false),
                    search_vi_plain = table.Column<string>(type: "text", nullable: false),
                    sources = table.Column<List<string>>(type: "text[]", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    edited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_words", x => x.id);
                    table.CheckConstraint("ck_words_han_viet_status", "han_viet_status IS NULL OR han_viet_status IN ('derived','reviewed')");
                    table.CheckConstraint("ck_words_hsk_exam2026_level", "hsk_exam2026_level IS NULL OR hsk_exam2026_level BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_words_hsk2_level", "hsk2_level IS NULL OR hsk2_level BETWEEN 1 AND 6");
                    table.CheckConstraint("ck_words_hsk3_level", "hsk3_level IS NULL OR hsk3_level BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_words_meaning_vi_source", "meaning_vi_source IN ('cvdict','machine','manual')");
                    table.CheckConstraint("ck_words_meaning_vi_status", "meaning_vi_status IN ('machine','reviewed')");
                });

            migrationBuilder.CreateTable(
                name: "word_characters",
                schema: "content",
                columns: table => new
                {
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<short>(type: "smallint", nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_word_characters", x => new { x.word_id, x.position });
                    table.ForeignKey(
                        name: "fk_word_characters_characters_character_id",
                        column: x => x.character_id,
                        principalSchema: "content",
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_word_characters_words_word_id",
                        column: x => x.word_id,
                        principalSchema: "content",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_characters_hanzi",
                schema: "content",
                table: "characters",
                column: "hanzi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_import_runs_dataset_started",
                schema: "content",
                table: "import_runs",
                columns: new[] { "dataset", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_word_characters_character",
                schema: "content",
                table: "word_characters",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "ix_words_han_viet_plain",
                schema: "content",
                table: "words",
                column: "han_viet_plain");

            migrationBuilder.CreateIndex(
                name: "ix_words_hsk2",
                schema: "content",
                table: "words",
                column: "hsk2_level");

            migrationBuilder.CreateIndex(
                name: "ix_words_level_path",
                schema: "content",
                table: "words",
                columns: new[] { "hsk3_level", "path_order" });

            migrationBuilder.CreateIndex(
                name: "ix_words_pinyin_compact",
                schema: "content",
                table: "words",
                column: "pinyin_compact")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_words_pinyin_search",
                schema: "content",
                table: "words",
                column: "pinyin_search")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_words_search_vi_plain_trgm",
                schema: "content",
                table: "words",
                column: "search_vi_plain")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_words_search_vi_trgm",
                schema: "content",
                table: "words",
                column: "search_vi")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_words_simplified_pattern",
                schema: "content",
                table: "words",
                column: "simplified")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_words_traditional",
                schema: "content",
                table: "words",
                column: "traditional");

            migrationBuilder.CreateIndex(
                name: "ux_words_simplified_pinyin",
                schema: "content",
                table: "words",
                columns: new[] { "simplified", "pinyin" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_runs",
                schema: "content");

            migrationBuilder.DropTable(
                name: "word_characters",
                schema: "content");

            migrationBuilder.DropTable(
                name: "characters",
                schema: "content");

            migrationBuilder.DropTable(
                name: "words",
                schema: "content");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
