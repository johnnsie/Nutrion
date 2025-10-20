using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpgradColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ColorId",
                table: "Player",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "Color",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Color_PlayerId",
                table: "Color",
                column: "PlayerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Color_Player_PlayerId",
                table: "Color",
                column: "PlayerId",
                principalTable: "Player",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Color_Player_PlayerId",
                table: "Color");

            migrationBuilder.DropIndex(
                name: "IX_Color_PlayerId",
                table: "Color");

            migrationBuilder.DropColumn(
                name: "ColorId",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Color");
        }
    }
}
