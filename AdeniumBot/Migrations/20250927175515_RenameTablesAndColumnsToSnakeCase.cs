using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdeniumBot.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesAndColumnsToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlacklistLinks_PlayerProfiles_OwnerId",
                table: "BlacklistLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_BlacklistLinks_PlayerProfiles_TargetId",
                table: "BlacklistLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteLinks_PlayerProfiles_OwnerId",
                table: "FavoriteLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteLinks_PlayerProfiles_TargetId",
                table: "FavoriteLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_player_quests_PlayerProfiles_PlayerId",
                table: "player_quests");

            migrationBuilder.DropForeignKey(
                name: "FK_player_quests_quests_QuestId",
                table: "player_quests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerProfiles",
                table: "PlayerProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_player_profiles_exp_nonneg",
                table: "PlayerProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FavoriteLinks",
                table: "FavoriteLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BlacklistLinks",
                table: "BlacklistLinks");

            migrationBuilder.RenameTable(
                name: "PlayerProfiles",
                newName: "player_profiles");

            migrationBuilder.RenameTable(
                name: "FavoriteLinks",
                newName: "favorite_links");

            migrationBuilder.RenameTable(
                name: "BlacklistLinks",
                newName: "blacklist_links");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "role_exp_rules",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "quests",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "QuestId",
                table: "player_quests",
                newName: "quest_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "player_quests",
                newName: "player_id");

            migrationBuilder.RenameIndex(
                name: "IX_player_quests_QuestId",
                table: "player_quests",
                newName: "IX_player_quests_quest_id");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "player_profiles",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Exp",
                table: "player_profiles",
                newName: "exp");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "player_profiles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "DiscordUserId",
                table: "player_profiles",
                newName: "discord_user_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "player_profiles",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerProfiles_DiscordUserId",
                table: "player_profiles",
                newName: "IX_player_profiles_discord_user_id");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                table: "favorite_links",
                newName: "target_id");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "favorite_links",
                newName: "owner_id");

            migrationBuilder.RenameIndex(
                name: "IX_FavoriteLinks_TargetId",
                table: "favorite_links",
                newName: "IX_favorite_links_target_id");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                table: "blacklist_links",
                newName: "target_id");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "blacklist_links",
                newName: "owner_id");

            migrationBuilder.RenameIndex(
                name: "IX_BlacklistLinks_TargetId",
                table: "blacklist_links",
                newName: "IX_blacklist_links_target_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_profiles",
                table: "player_profiles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_favorite_links",
                table: "favorite_links",
                columns: new[] { "owner_id", "target_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_blacklist_links",
                table: "blacklist_links",
                columns: new[] { "owner_id", "target_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_player_profiles_exp_nonneg",
                table: "player_profiles",
                sql: "exp >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_blacklist_links_player_profiles_owner_id",
                table: "blacklist_links",
                column: "owner_id",
                principalTable: "player_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_blacklist_links_player_profiles_target_id",
                table: "blacklist_links",
                column: "target_id",
                principalTable: "player_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_favorite_links_player_profiles_owner_id",
                table: "favorite_links",
                column: "owner_id",
                principalTable: "player_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_favorite_links_player_profiles_target_id",
                table: "favorite_links",
                column: "target_id",
                principalTable: "player_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_quests_player_profiles_player_id",
                table: "player_quests",
                column: "player_id",
                principalTable: "player_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_quests_quests_quest_id",
                table: "player_quests",
                column: "quest_id",
                principalTable: "quests",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blacklist_links_player_profiles_owner_id",
                table: "blacklist_links");

            migrationBuilder.DropForeignKey(
                name: "FK_blacklist_links_player_profiles_target_id",
                table: "blacklist_links");

            migrationBuilder.DropForeignKey(
                name: "FK_favorite_links_player_profiles_owner_id",
                table: "favorite_links");

            migrationBuilder.DropForeignKey(
                name: "FK_favorite_links_player_profiles_target_id",
                table: "favorite_links");

            migrationBuilder.DropForeignKey(
                name: "FK_player_quests_player_profiles_player_id",
                table: "player_quests");

            migrationBuilder.DropForeignKey(
                name: "FK_player_quests_quests_quest_id",
                table: "player_quests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_profiles",
                table: "player_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_player_profiles_exp_nonneg",
                table: "player_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_favorite_links",
                table: "favorite_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_blacklist_links",
                table: "blacklist_links");

            migrationBuilder.RenameTable(
                name: "player_profiles",
                newName: "PlayerProfiles");

            migrationBuilder.RenameTable(
                name: "favorite_links",
                newName: "FavoriteLinks");

            migrationBuilder.RenameTable(
                name: "blacklist_links",
                newName: "BlacklistLinks");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "role_exp_rules",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "quests",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "quest_id",
                table: "player_quests",
                newName: "QuestId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "player_quests",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_player_quests_quest_id",
                table: "player_quests",
                newName: "IX_player_quests_QuestId");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "PlayerProfiles",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "exp",
                table: "PlayerProfiles",
                newName: "Exp");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "PlayerProfiles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "discord_user_id",
                table: "PlayerProfiles",
                newName: "DiscordUserId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "PlayerProfiles",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_player_profiles_discord_user_id",
                table: "PlayerProfiles",
                newName: "IX_PlayerProfiles_DiscordUserId");

            migrationBuilder.RenameColumn(
                name: "target_id",
                table: "FavoriteLinks",
                newName: "TargetId");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "FavoriteLinks",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_favorite_links_target_id",
                table: "FavoriteLinks",
                newName: "IX_FavoriteLinks_TargetId");

            migrationBuilder.RenameColumn(
                name: "target_id",
                table: "BlacklistLinks",
                newName: "TargetId");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "BlacklistLinks",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_blacklist_links_target_id",
                table: "BlacklistLinks",
                newName: "IX_BlacklistLinks_TargetId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerProfiles",
                table: "PlayerProfiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavoriteLinks",
                table: "FavoriteLinks",
                columns: new[] { "OwnerId", "TargetId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BlacklistLinks",
                table: "BlacklistLinks",
                columns: new[] { "OwnerId", "TargetId" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_player_profiles_exp_nonneg",
                table: "PlayerProfiles",
                sql: "\"Exp\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_BlacklistLinks_PlayerProfiles_OwnerId",
                table: "BlacklistLinks",
                column: "OwnerId",
                principalTable: "PlayerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BlacklistLinks_PlayerProfiles_TargetId",
                table: "BlacklistLinks",
                column: "TargetId",
                principalTable: "PlayerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteLinks_PlayerProfiles_OwnerId",
                table: "FavoriteLinks",
                column: "OwnerId",
                principalTable: "PlayerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteLinks_PlayerProfiles_TargetId",
                table: "FavoriteLinks",
                column: "TargetId",
                principalTable: "PlayerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_quests_PlayerProfiles_PlayerId",
                table: "player_quests",
                column: "PlayerId",
                principalTable: "PlayerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_quests_quests_QuestId",
                table: "player_quests",
                column: "QuestId",
                principalTable: "quests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
