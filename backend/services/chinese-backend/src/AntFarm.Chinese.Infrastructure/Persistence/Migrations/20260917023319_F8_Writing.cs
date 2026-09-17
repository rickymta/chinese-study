using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Chinese.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F8_Writing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_characters_hanzi",
                schema: "content",
                table: "characters");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_characters_hanzi",
                schema: "content",
                table: "characters",
                column: "hanzi");

            migrationBuilder.CreateTable(
                name: "character_writing_stats",
                schema: "learning",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hanzi = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guided_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    recall_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_mode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    last_mistakes = table.Column<short>(type: "smallint", nullable: false),
                    last_hints = table.Column<short>(type: "smallint", nullable: false),
                    best_recall_mistakes = table.Column<short>(type: "smallint", nullable: true),
                    clean_recall_days = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    last_clean_recall_date = table.Column<DateOnly>(type: "date", nullable: true),
                    first_practiced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_practiced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_character_writing_stats", x => new { x.user_id, x.hanzi });
                    table.CheckConstraint("ck_char_writing_stats_last_mode", "last_mode IN ('guided','recall')");
                    table.ForeignKey(
                        name: "fk_character_writing_stats_characters_hanzi",
                        column: x => x.hanzi,
                        principalSchema: "content",
                        principalTable: "characters",
                        principalColumn: "hanzi",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_character_writing_stats_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "writing_attempts",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hanzi = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    mode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    total_strokes = table.Column<short>(type: "smallint", nullable: false),
                    total_mistakes = table.Column<short>(type: "smallint", nullable: false),
                    hints_used = table.Column<short>(type: "smallint", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    is_clean = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_writing_attempts", x => x.id);
                    table.CheckConstraint("ck_writing_attempts_duration_ms", "duration_ms IS NULL OR duration_ms BETWEEN 0 AND 3600000");
                    table.CheckConstraint("ck_writing_attempts_hints_used", "hints_used BETWEEN 0 AND 200");
                    table.CheckConstraint("ck_writing_attempts_mode", "mode IN ('guided','recall')");
                    table.CheckConstraint("ck_writing_attempts_total_mistakes", "total_mistakes BETWEEN 0 AND 500");
                    table.CheckConstraint("ck_writing_attempts_total_strokes", "total_strokes BETWEEN 1 AND 64");
                    table.ForeignKey(
                        name: "fk_writing_attempts_characters_hanzi",
                        column: x => x.hanzi,
                        principalSchema: "content",
                        principalTable: "characters",
                        principalColumn: "hanzi",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_writing_attempts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_char_writing_stats_user_last",
                schema: "learning",
                table: "character_writing_stats",
                columns: new[] { "user_id", "last_practiced_at" });

            migrationBuilder.CreateIndex(
                name: "ix_character_writing_stats_hanzi",
                schema: "learning",
                table: "character_writing_stats",
                column: "hanzi");

            migrationBuilder.CreateIndex(
                name: "ix_writing_attempts_hanzi",
                schema: "learning",
                table: "writing_attempts",
                column: "hanzi");

            migrationBuilder.CreateIndex(
                name: "ix_writing_attempts_user_completed",
                schema: "learning",
                table: "writing_attempts",
                columns: new[] { "user_id", "completed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_writing_attempts_user_hanzi",
                schema: "learning",
                table: "writing_attempts",
                columns: new[] { "user_id", "hanzi", "completed_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_writing_attempts_client_attempt_id",
                schema: "learning",
                table: "writing_attempts",
                column: "client_attempt_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_writing_stats",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "writing_attempts",
                schema: "learning");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_characters_hanzi",
                schema: "content",
                table: "characters");

            migrationBuilder.CreateIndex(
                name: "ux_characters_hanzi",
                schema: "content",
                table: "characters",
                column: "hanzi",
                unique: true);
        }
    }
}
