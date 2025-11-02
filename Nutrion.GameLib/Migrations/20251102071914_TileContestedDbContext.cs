using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class TileContestedDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTile_Player_PlayersId",
                table: "PlayerTile");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTile_Tile_TilesId",
                table: "PlayerTile");

            migrationBuilder.RenameColumn(
                name: "TilesId",
                table: "PlayerTile",
                newName: "TileId");

            migrationBuilder.RenameColumn(
                name: "PlayersId",
                table: "PlayerTile",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerTile_TilesId",
                table: "PlayerTile",
                newName: "IX_PlayerTile_TileId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTile_Player_PlayerId",
                table: "PlayerTile",
                column: "PlayerId",
                principalTable: "Player",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTile_Tile_TileId",
                table: "PlayerTile",
                column: "TileId",
                principalTable: "Tile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTile_Player_PlayerId",
                table: "PlayerTile");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTile_Tile_TileId",
                table: "PlayerTile");

            migrationBuilder.RenameColumn(
                name: "TileId",
                table: "PlayerTile",
                newName: "TilesId");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "PlayerTile",
                newName: "PlayersId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerTile_TileId",
                table: "PlayerTile",
                newName: "IX_PlayerTile_TilesId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTile_Player_PlayersId",
                table: "PlayerTile",
                column: "PlayersId",
                principalTable: "Player",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTile_Tile_TilesId",
                table: "PlayerTile",
                column: "TilesId",
                principalTable: "Tile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
