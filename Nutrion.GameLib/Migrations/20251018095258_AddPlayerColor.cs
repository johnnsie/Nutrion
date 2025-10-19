using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerColorId",
                table: "Player",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerColorId",
                table: "Player");
        }
    }
}
