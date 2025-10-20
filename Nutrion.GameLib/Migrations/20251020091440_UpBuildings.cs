using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpBuildings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BuildingId",
                table: "Tile",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BuildingType",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GLTFModelPath",
                table: "BuildingType",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TileRadius",
                table: "BuildingType",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginTileId",
                table: "Building",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tile_BuildingId",
                table: "Tile",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Building_OriginTileId",
                table: "Building",
                column: "OriginTileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Building_Tile_OriginTileId",
                table: "Building",
                column: "OriginTileId",
                principalTable: "Tile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tile_Building_BuildingId",
                table: "Tile",
                column: "BuildingId",
                principalTable: "Building",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Building_Tile_OriginTileId",
                table: "Building");

            migrationBuilder.DropForeignKey(
                name: "FK_Tile_Building_BuildingId",
                table: "Tile");

            migrationBuilder.DropIndex(
                name: "IX_Tile_BuildingId",
                table: "Tile");

            migrationBuilder.DropIndex(
                name: "IX_Building_OriginTileId",
                table: "Building");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "Tile");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BuildingType");

            migrationBuilder.DropColumn(
                name: "GLTFModelPath",
                table: "BuildingType");

            migrationBuilder.DropColumn(
                name: "TileRadius",
                table: "BuildingType");

            migrationBuilder.DropColumn(
                name: "OriginTileId",
                table: "Building");
        }
    }
}
