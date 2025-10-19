using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class TileSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "Tile",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tile_PlayerId",
                table: "Tile",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tile_Player_PlayerId",
                table: "Tile",
                column: "PlayerId",
                principalTable: "Player",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tile_Player_PlayerId",
                table: "Tile");

            migrationBuilder.DropIndex(
                name: "IX_Tile_PlayerId",
                table: "Tile");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Tile");
        }
    }
}
