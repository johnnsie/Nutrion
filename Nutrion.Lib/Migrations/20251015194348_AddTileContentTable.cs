using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTileContentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TileContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TileId = table.Column<int>(type: "integer", nullable: false),
                    GLTFComponent = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TileContent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TileContent_Tile_TileId",
                        column: x => x.TileId,
                        principalTable: "Tile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TileContent_TileId",
                table: "TileContent",
                column: "TileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TileContent");
        }
    }
}
