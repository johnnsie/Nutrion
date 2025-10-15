using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class ColorNowRandom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PlayerColor",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PlayerColor",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PlayerColor",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "PlayerColor",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PlayerColor",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "PlayerColor",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "PlayerColor",
                keyColumn: "Id",
                keyValue: 7);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
