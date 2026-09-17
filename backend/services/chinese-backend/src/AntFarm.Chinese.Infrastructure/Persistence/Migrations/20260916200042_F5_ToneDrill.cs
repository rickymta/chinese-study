using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Chinese.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F5_ToneDrill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "learning");

            migrationBuilder.CreateTable(
                name: "study_events",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    correct = table.Column<int>(type: "integer", nullable: true),
                    ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_events", x => x.id);
                    table.CheckConstraint("ck_study_events_correct", "correct IS NULL OR (correct >= 0 AND correct <= quantity)");
                    table.CheckConstraint("ck_study_events_quantity", "quantity >= 0");
                    table.ForeignKey(
                        name: "fk_study_events_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tone_drill_sessions",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total = table.Column<short>(type: "smallint", nullable: false),
                    correct = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tone_drill_sessions", x => x.id);
                    table.CheckConstraint("ck_tone_drill_sessions_correct", "correct BETWEEN 0 AND total");
                    table.CheckConstraint("ck_tone_drill_sessions_finished_after_started", "finished_at >= started_at");
                    table.CheckConstraint("ck_tone_drill_sessions_mode", "mode IN ('listen_tone','tone_pair')");
                    table.CheckConstraint("ck_tone_drill_sessions_total", "total BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "fk_tone_drill_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tone_drill_answers",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_index = table.Column<short>(type: "smallint", nullable: false),
                    part_index = table.Column<short>(type: "smallint", nullable: false),
                    syllable = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    hanzi = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    expected_tone = table.Column<short>(type: "smallint", nullable: false),
                    answered_tone = table.Column<short>(type: "smallint", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    response_ms = table.Column<int>(type: "integer", nullable: true),
                    replay_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tone_drill_answers", x => x.id);
                    table.CheckConstraint("ck_tone_drill_answers_answered_tone", "answered_tone BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_tone_drill_answers_expected_tone", "expected_tone BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_tone_drill_answers_item_index", "item_index >= 0");
                    table.CheckConstraint("ck_tone_drill_answers_part_index", "part_index IN (0,1)");
                    table.CheckConstraint("ck_tone_drill_answers_replay_count", "replay_count BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_tone_drill_answers_response_ms", "response_ms IS NULL OR response_ms BETWEEN 0 AND 600000");
                    table.ForeignKey(
                        name: "fk_tone_drill_answers_tone_drill_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "learning",
                        principalTable: "tone_drill_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_study_events_user_id_kind_occurred_at",
                schema: "learning",
                table: "study_events",
                columns: new[] { "user_id", "kind", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_study_events_user_id_local_date",
                schema: "learning",
                table: "study_events",
                columns: new[] { "user_id", "local_date" });

            migrationBuilder.CreateIndex(
                name: "ix_tone_drill_answers_session_id_item_index_part_index",
                schema: "learning",
                table: "tone_drill_answers",
                columns: new[] { "session_id", "item_index", "part_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tone_drill_answers_user_id_expected_tone_answered_at_part_i",
                schema: "learning",
                table: "tone_drill_answers",
                columns: new[] { "user_id", "expected_tone", "answered_at", "part_index" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_tone_drill_sessions_user_id_finished_at",
                schema: "learning",
                table: "tone_drill_sessions",
                columns: new[] { "user_id", "finished_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_tone_drill_sessions_user_id_client_session_id",
                schema: "learning",
                table: "tone_drill_sessions",
                columns: new[] { "user_id", "client_session_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_events",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "tone_drill_answers",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "tone_drill_sessions",
                schema: "learning");
        }
    }
}
