﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdeniumBot.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleNameToRoleExpRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role_name",
                table: "role_exp_rules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role_name",
                table: "role_exp_rules");
        }
    }
}
