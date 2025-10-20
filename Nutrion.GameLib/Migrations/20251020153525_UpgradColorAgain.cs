using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpgradColorAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Color_Player_PlayerId",
                table: "Color");

            migrationBuilder.DropIndex(
                name: "IX_Color_PlayerId",
                table: "Color");

            migrationBuilder.CreateIndex(
                name: "IX_Player_ColorId",
                table: "Player",
                column: "ColorId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Player_Color_ColorId",
                table: "Player",
                column: "ColorId",
                principalTable: "Color",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Player_Color_ColorId",
                table: "Player");

            migrationBuilder.DropIndex(
                name: "IX_Player_ColorId",
                table: "Player");

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
    }
}
