using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Chinese.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F9_Lessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lessons",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    topic = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "hsk1"),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    objectives = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    estimated_minutes = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)15),
                    glossary = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    review_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "machine"),
                    source = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    source_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    edited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.id);
                    table.CheckConstraint("ck_lessons_estimated_minutes", "estimated_minutes BETWEEN 1 AND 120");
                    table.CheckConstraint("ck_lessons_level", "level IN ('hsk1')");
                    table.CheckConstraint("ck_lessons_review_status", "review_status IN ('machine','reviewed')");
                    table.CheckConstraint("ck_lessons_source", "source IN ('seed','admin')");
                    table.CheckConstraint("ck_lessons_status", "status IN ('draft','published','archived')");
                    table.ForeignKey(
                        name: "fk_lessons_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lessons_users_edited_by",
                        column: x => x.edited_by,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lessons_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "lesson_blocks",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<short>(type: "smallint", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_blocks", x => x.id);
                    table.CheckConstraint("ck_lesson_blocks_type", "type IN ('text','dialogue','grammar','tip')");
                    table.ForeignKey(
                        name: "fk_lesson_blocks_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "content",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson_progress",
                schema: "learning",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    best_score_percent = table.Column<short>(type: "smallint", nullable: true),
                    attempts_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_progress", x => new { x.user_id, x.lesson_id });
                    table.CheckConstraint("ck_lesson_progress_best_score", "best_score_percent IS NULL OR best_score_percent BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_lesson_progress_status", "status IN ('in_progress','completed')");
                    table.ForeignKey(
                        name: "fk_lesson_progress_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "content",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lesson_progress_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson_words",
                schema: "content",
                columns: table => new
                {
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_words", x => new { x.lesson_id, x.word_id });
                    table.ForeignKey(
                        name: "fk_lesson_words_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "content",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lesson_words_words_word_id",
                        column: x => x.word_id,
                        principalSchema: "content",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quiz_attempts",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    total = table.Column<short>(type: "smallint", nullable: false),
                    correct = table.Column<short>(type: "smallint", nullable: false),
                    score_percent = table.Column<short>(type: "smallint", nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    answers = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_attempts", x => x.id);
                    table.CheckConstraint("ck_quiz_attempts_correct", "correct BETWEEN 0 AND total");
                    table.CheckConstraint("ck_quiz_attempts_score_percent", "score_percent BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_quiz_attempts_total", "total > 0");
                    table.ForeignKey(
                        name: "fk_quiz_attempts_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "content",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quiz_attempts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quiz_questions",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    order_index = table.Column<short>(type: "smallint", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    prompt = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    prompt_lang = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    prompt_pinyin = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    audio_text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    options = table.Column<string>(type: "jsonb", nullable: false),
                    correct_option_id = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_questions", x => x.id);
                    table.CheckConstraint("ck_quiz_questions_prompt_lang", "prompt_lang IN ('vi','zh')");
                    table.CheckConstraint("ck_quiz_questions_type", "type IN ('single_choice','listen_choice')");
                    table.ForeignKey(
                        name: "fk_quiz_questions_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "content",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lesson_blocks_lesson",
                schema: "content",
                table: "lesson_blocks",
                columns: new[] { "lesson_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ix_lesson_progress_lesson_id",
                schema: "learning",
                table: "lesson_progress",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_lesson_progress_user_status",
                schema: "learning",
                table: "lesson_progress",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_lesson_words_word",
                schema: "content",
                table: "lesson_words",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_created_by",
                schema: "content",
                table: "lessons",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_edited_by",
                schema: "content",
                table: "lessons",
                column: "edited_by");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_reviewed_by",
                schema: "content",
                table: "lessons",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_status_order",
                schema: "content",
                table: "lessons",
                columns: new[] { "status", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ux_lessons_slug",
                schema: "content",
                table: "lessons",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quiz_attempts_lesson_id",
                schema: "learning",
                table: "quiz_attempts",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_quiz_attempts_user_lesson",
                schema: "learning",
                table: "quiz_attempts",
                columns: new[] { "user_id", "lesson_id", "submitted_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_quiz_attempts_client_attempt_id",
                schema: "learning",
                table: "quiz_attempts",
                column: "client_attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quiz_questions_lesson",
                schema: "content",
                table: "quiz_questions",
                columns: new[] { "lesson_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ux_quiz_questions_lesson_key",
                schema: "content",
                table: "quiz_questions",
                columns: new[] { "lesson_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lesson_blocks",
                schema: "content");

            migrationBuilder.DropTable(
                name: "lesson_progress",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "lesson_words",
                schema: "content");

            migrationBuilder.DropTable(
                name: "quiz_attempts",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "quiz_questions",
                schema: "content");

            migrationBuilder.DropTable(
                name: "lessons",
                schema: "content");
        }
    }
}
