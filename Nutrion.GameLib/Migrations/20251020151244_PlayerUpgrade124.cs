using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerUpgrade124 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.DropForeignKey(
                name: "FK_Player_PlayerColor_PlayerColorId",
                table: "Player");
            */
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerColor",
                table: "PlayerColor");

            /*
            migrationBuilder.DropIndex(
                name: "IX_Player_PlayerColorId",
                table: "Player");
            */

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PlayerColor");

            /*
            migrationBuilder.DropColumn(
                name: "PlayerColorId",
                table: "Player");
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "PlayerColor",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerColorId",
                table: "Player",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerColor",
                table: "PlayerColor",
                column: "Id");

            /*
            migrationBuilder.CreateIndex(
                name: "IX_Player_PlayerColorId",
                table: "Player",
                column: "PlayerColorId");
            */
            /*
            migrationBuilder.AddForeignKey(
                name: "FK_Player_PlayerColor_PlayerColorId",
                table: "Player",
                column: "PlayerColorId",
                principalTable: "PlayerColor",
                principalColumn: "Id");
            */
        }
    }
}
