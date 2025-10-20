using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerUpgrade123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerColor_Player_PlayerId",
                table: "PlayerColor");

            migrationBuilder.DropIndex(
                name: "IX_PlayerColor_PlayerId",
                table: "PlayerColor");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "PlayerColor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "PlayerColor",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerColor_PlayerId",
                table: "PlayerColor",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerColor_Player_PlayerId",
                table: "PlayerColor",
                column: "PlayerId",
                principalTable: "Player",
                principalColumn: "Id");
        }
    }
}
