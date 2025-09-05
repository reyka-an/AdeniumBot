using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdeniumBot.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "__EFMigrationsHistory");
            migrationBuilder.DropTable(name: "BlacklistLinks");
            migrationBuilder.DropTable(name: "FavoriteLinks");
            migrationBuilder.DropTable(name: "PlayerProfiles");
            
            migrationBuilder.CreateTable(
                name: "PlayerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscordUserId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Exp = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlacklistLinks",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistLinks", x => new { x.OwnerId, x.TargetId });
                    table.ForeignKey(
                        name: "FK_BlacklistLinks_PlayerProfiles_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlacklistLinks_PlayerProfiles_TargetId",
                        column: x => x.TargetId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FavoriteLinks",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteLinks", x => new { x.OwnerId, x.TargetId });
                    table.ForeignKey(
                        name: "FK_FavoriteLinks_PlayerProfiles_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FavoriteLinks_PlayerProfiles_TargetId",
                        column: x => x.TargetId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistLinks_TargetId",
                table: "BlacklistLinks",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteLinks_TargetId",
                table: "FavoriteLinks",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProfiles_DiscordUserId",
                table: "PlayerProfiles",
                column: "DiscordUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlacklistLinks");

            migrationBuilder.DropTable(
                name: "FavoriteLinks");

            migrationBuilder.DropTable(
                name: "PlayerProfiles");
        }
    }
}
