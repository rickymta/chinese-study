using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntFarm.Chinese.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F7_Srs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learner_settings",
                schema: "learning",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    daily_new_cards = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)10),
                    daily_review_limit = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)200),
                    desired_retention = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0.90m),
                    tts_rate = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0.80m),
                    auto_play_audio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learner_settings", x => x.user_id);
                    table.CheckConstraint("ck_learner_settings_daily_new_cards", "daily_new_cards BETWEEN 0 AND 50");
                    table.CheckConstraint("ck_learner_settings_daily_review_limit", "daily_review_limit BETWEEN 10 AND 1000");
                    table.CheckConstraint("ck_learner_settings_desired_retention", "desired_retention BETWEEN 0.80 AND 0.97");
                    table.CheckConstraint("ck_learner_settings_tts_rate", "tts_rate BETWEEN 0.50 AND 1.20");
                    table.ForeignKey(
                        name: "fk_learner_settings_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srs_cards",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false, defaultValue: "hanzi_to_meaning"),
                    state = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    step = table.Column<short>(type: "smallint", nullable: true),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stability = table.Column<double>(type: "double precision", nullable: true),
                    difficulty = table.Column<double>(type: "double precision", nullable: true),
                    reps = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lapses = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_review_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    first_reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    first_reviewed_local_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_srs_cards", x => x.id);
                    table.CheckConstraint("ck_srs_cards_card_type", "card_type IN ('hanzi_to_meaning')");
                    table.CheckConstraint("ck_srs_cards_source", "source IN ('path','manual','lesson')");
                    table.CheckConstraint("ck_srs_cards_state", "state IN ('new','learning','review','relearning')");
                    table.ForeignKey(
                        name: "fk_srs_cards_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "access",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_srs_cards_words_word_id",
                        column: x => x.word_id,
                        principalSchema: "content",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srs_review_logs",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    state_before = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    step_before = table.Column<short>(type: "smallint", nullable: true),
                    stability_before = table.Column<double>(type: "double precision", nullable: true),
                    difficulty_before = table.Column<double>(type: "double precision", nullable: true),
                    state_after = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    stability_after = table.Column<double>(type: "double precision", nullable: false),
                    difficulty_after = table.Column<double>(type: "double precision", nullable: false),
                    due_after = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    elapsed_days = table.Column<double>(type: "double precision", nullable: false),
                    scheduled_days = table.Column<double>(type: "double precision", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_srs_review_logs", x => x.id);
                    table.CheckConstraint("ck_srs_review_logs_rating", "rating BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_srs_review_logs_srs_cards_card_id",
                        column: x => x.card_id,
                        principalSchema: "learning",
                        principalTable: "srs_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_srs_cards_user_first_date",
                schema: "learning",
                table: "srs_cards",
                columns: new[] { "user_id", "first_reviewed_local_date" });

            migrationBuilder.CreateIndex(
                name: "ix_srs_cards_user_state_due",
                schema: "learning",
                table: "srs_cards",
                columns: new[] { "user_id", "state", "due_at" },
                filter: "is_suspended = false");

            migrationBuilder.CreateIndex(
                name: "ix_srs_cards_word_id",
                schema: "learning",
                table: "srs_cards",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ux_srs_cards_user_word_type",
                schema: "learning",
                table: "srs_cards",
                columns: new[] { "user_id", "word_id", "card_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_srs_review_logs_card",
                schema: "learning",
                table: "srs_review_logs",
                columns: new[] { "card_id", "reviewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_srs_review_logs_user_date",
                schema: "learning",
                table: "srs_review_logs",
                columns: new[] { "user_id", "local_date" });

            migrationBuilder.CreateIndex(
                name: "ux_srs_review_logs_client_review_id",
                schema: "learning",
                table: "srs_review_logs",
                column: "client_review_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learner_settings",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "srs_review_logs",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "srs_cards",
                schema: "learning");
        }
    }
}
