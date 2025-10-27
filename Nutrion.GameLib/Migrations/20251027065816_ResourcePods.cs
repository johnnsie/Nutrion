using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResourcePods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GLTFModelPath",
                table: "Resource",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "Resource",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "OriginTileId",
                table: "Resource",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resource_OriginTileId",
                table: "Resource",
                column: "OriginTileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resource_Tile_OriginTileId",
                table: "Resource",
                column: "OriginTileId",
                principalTable: "Tile",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resource_Tile_OriginTileId",
                table: "Resource");

            migrationBuilder.DropIndex(
                name: "IX_Resource_OriginTileId",
                table: "Resource");

            migrationBuilder.DropColumn(
                name: "GLTFModelPath",
                table: "Resource");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Resource");

            migrationBuilder.DropColumn(
                name: "OriginTileId",
                table: "Resource");
        }
    }
}
