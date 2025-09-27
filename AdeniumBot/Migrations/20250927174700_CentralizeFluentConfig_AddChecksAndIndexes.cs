using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdeniumBot.Migrations
{
    /// <inheritdoc />
    public partial class CentralizeFluentConfig_AddChecksAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "role_exp_rules",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "PlayerProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "ck_role_exp_rules_amount_nonneg",
                table: "role_exp_rules",
                sql: "exp_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_quests_exp_reward_nonneg",
                table: "quests",
                sql: "exp_reward >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_quests_max_comp_valid",
                table: "quests",
                sql: "max_completions_per_player IS NULL OR max_completions_per_player > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_player_profiles_exp_nonneg",
                table: "PlayerProfiles",
                sql: "\"Exp\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_player_quests_completed_nonneg",
                table: "player_quests",
                sql: "completed_count >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_role_exp_rules_amount_nonneg",
                table: "role_exp_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_quests_exp_reward_nonneg",
                table: "quests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_quests_max_comp_valid",
                table: "quests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_player_profiles_exp_nonneg",
                table: "PlayerProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_player_quests_completed_nonneg",
                table: "player_quests");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "role_exp_rules",
                newName: "id");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "PlayerProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
