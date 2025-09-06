using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdeniumBot.Migrations
{
    /// <inheritdoc />
    public partial class AddQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    exp_reward = table.Column<int>(type: "integer", nullable: false),
                    max_completions_per_player = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_quests",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    QuestId = table.Column<int>(type: "integer", nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: false),
                    last_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_quests", x => new { x.PlayerId, x.QuestId });
                    table.ForeignKey(
                        name: "FK_player_quests_PlayerProfiles_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_quests_quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_quests_QuestId",
                table: "player_quests",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_quests_number",
                table: "quests",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_quests");

            migrationBuilder.DropTable(
                name: "quests");
        }
    }
}
