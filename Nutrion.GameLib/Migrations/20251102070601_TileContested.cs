using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class TileContested : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "PlayerTile",
                columns: table => new
                {
                    PlayersId = table.Column<Guid>(type: "uuid", nullable: false),
                    TilesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTile", x => new { x.PlayersId, x.TilesId });
                    table.ForeignKey(
                        name: "FK_PlayerTile_Player_PlayersId",
                        column: x => x.PlayersId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTile_Tile_TilesId",
                        column: x => x.TilesId,
                        principalTable: "Tile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTile_TilesId",
                table: "PlayerTile",
                column: "TilesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerTile");

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
    }
}
