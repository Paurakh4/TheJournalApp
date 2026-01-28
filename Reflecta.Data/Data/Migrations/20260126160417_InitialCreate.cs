using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Reflecta.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "moods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    emoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "export_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    export_format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date_range_start = table.Column<DateOnly>(type: "date", nullable: false),
                    date_range_end = table.Column<DateOnly>(type: "date", nullable: false),
                    entries_count = table.Column<int>(type: "integer", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    exported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_export_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    primary_mood_id = table.Column<int>(type: "integer", nullable: false),
                    word_count = table.Column<int>(type: "integer", nullable: false),
                    is_favorite = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_entries_moods_primary_mood_id",
                        column: x => x.primary_mood_id,
                        principalTable: "moods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "streaks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    longest_streak = table.Column<int>(type: "integer", nullable: false),
                    last_entry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    streak_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streaks", x => x.id);
                    table.ForeignKey(
                        name: "FK_streaks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_tags_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pin_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    pin_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    reminder_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    reminder_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entry_secondary_moods",
                columns: table => new
                {
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mood_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry_secondary_moods", x => new { x.entry_id, x.mood_id });
                    table.ForeignKey(
                        name: "FK_entry_secondary_moods_journal_entries_entry_id",
                        column: x => x.entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entry_secondary_moods_moods_mood_id",
                        column: x => x.mood_id,
                        principalTable: "moods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entry_tags",
                columns: table => new
                {
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry_tags", x => new { x.entry_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_entry_tags_journal_entries_entry_id",
                        column: x => x.entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entry_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "moods",
                columns: new[] { "id", "category", "display_order", "emoji", "is_active", "name" },
                values: new object[,]
                {
                    { 1, "Positive", 1, "😊", true, "Happy" },
                    { 2, "Positive", 2, "🤩", true, "Excited" },
                    { 3, "Positive", 3, "🙏", true, "Grateful" },
                    { 4, "Positive", 4, "😌", true, "Calm" },
                    { 5, "Positive", 5, "🥰", true, "Loved" },
                    { 6, "Positive", 6, "💪", true, "Proud" },
                    { 7, "Neutral", 7, "😐", true, "Okay" },
                    { 8, "Neutral", 8, "😴", true, "Tired" },
                    { 9, "Neutral", 9, "🤔", true, "Thinking" },
                    { 10, "Neutral", 10, "😑", true, "Bored" },
                    { 11, "Negative", 11, "😢", true, "Sad" },
                    { 12, "Negative", 12, "😰", true, "Anxious" },
                    { 13, "Negative", 13, "😠", true, "Angry" },
                    { 14, "Negative", 14, "😫", true, "Stressed" },
                    { 15, "Negative", 15, "😔", true, "Lonely" }
                });

            migrationBuilder.InsertData(
                table: "tags",
                columns: new[] { "id", "color", "created_at", "is_active", "is_system", "name", "user_id" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "#4A90D9", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(4810), true, true, "Work", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "#7ED321", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5350), true, true, "Personal", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "#F5A623", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5360), true, true, "Health", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "#D0021B", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5360), true, true, "Family", null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "#9013FE", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5370), true, true, "Travel", null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "#50E3C2", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5420), true, true, "Goals", null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "#BD10E0", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5420), true, true, "Learning", null },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "#FF6B6B", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5430), true, true, "Relationships", null },
                    { new Guid("99999999-9999-9999-9999-999999999999"), "#4ECDC4", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5430), true, true, "Mindfulness", null },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "#FFE66D", new DateTime(2026, 1, 26, 16, 4, 17, 29, DateTimeKind.Utc).AddTicks(5430), true, true, "Creativity", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_entry_secondary_moods_mood_id",
                table: "entry_secondary_moods",
                column: "mood_id");

            migrationBuilder.CreateIndex(
                name: "IX_entry_tags_tag_id",
                table: "entry_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_export_logs_user_id",
                table: "export_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_primary_mood_id",
                table: "journal_entries",
                column: "primary_mood_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_user_id_entry_date",
                table: "journal_entries",
                columns: new[] { "user_id", "entry_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_streaks_user_id",
                table: "streaks",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_name_user_id",
                table: "tags",
                columns: new[] { "name", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_user_id",
                table: "tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entry_secondary_moods");

            migrationBuilder.DropTable(
                name: "entry_tags");

            migrationBuilder.DropTable(
                name: "export_logs");

            migrationBuilder.DropTable(
                name: "streaks");

            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropTable(
                name: "journal_entries");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "moods");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
