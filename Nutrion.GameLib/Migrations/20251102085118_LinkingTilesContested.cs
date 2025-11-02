using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkingTilesContested : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Player_Tile_TileId",
                table: "Player");

            migrationBuilder.DropIndex(
                name: "IX_Player_TileId",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "TileId",
                table: "Player");

            migrationBuilder.CreateTable(
                name: "PlayerTile",
                columns: table => new
                {
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TileId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTile", x => new { x.PlayerId, x.TileId });
                    table.ForeignKey(
                        name: "FK_PlayerTile_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTile_Tile_TileId",
                        column: x => x.TileId,
                        principalTable: "Tile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTile_TileId",
                table: "PlayerTile",
                column: "TileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerTile");

            migrationBuilder.AddColumn<int>(
                name: "TileId",
                table: "Player",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Player_TileId",
                table: "Player",
                column: "TileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Player_Tile_TileId",
                table: "Player",
                column: "TileId",
                principalTable: "Tile",
                principalColumn: "Id");
        }
    }
}
