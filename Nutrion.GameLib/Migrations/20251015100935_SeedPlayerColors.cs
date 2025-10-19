using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedPlayerColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Player");

            migrationBuilder.CreateTable(
                name: "PlayerColor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HexCode = table.Column<string>(type: "text", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerColor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerColor_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "PlayerColor",
                columns: new[] { "Id", "HexCode", "PlayerId" },
                values: new object[,]
                {
                    { 1, "#FF5733", null },
                    { 2, "#33FF57", null },
                    { 3, "#3357FF", null },
                    { 4, "#FFD700", null },
                    { 5, "#FF69B4", null },
                    { 6, "#00CED1", null },
                    { 7, "#800080", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerColor_PlayerId",
                table: "PlayerColor",
                column: "PlayerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerColor");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Player",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
