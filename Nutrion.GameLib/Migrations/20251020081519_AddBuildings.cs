using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenAIRequest");

            migrationBuilder.AddColumn<Guid>(
                name: "BuildingCostId",
                table: "Resource",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BuildingCost",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    LevelMultiplier = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingCost", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuildingType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BuildingCostId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildingType_BuildingCost_BuildingCostId",
                        column: x => x.BuildingCostId,
                        principalTable: "BuildingCost",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Building",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BuildingTypeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Building", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Building_BuildingType_BuildingTypeId",
                        column: x => x.BuildingTypeId,
                        principalTable: "BuildingType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Building_Player_PlayerOwnerId",
                        column: x => x.PlayerOwnerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resource_BuildingCostId",
                table: "Resource",
                column: "BuildingCostId");

            migrationBuilder.CreateIndex(
                name: "IX_Building_BuildingTypeId",
                table: "Building",
                column: "BuildingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Building_PlayerOwnerId",
                table: "Building",
                column: "PlayerOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingType_BuildingCostId",
                table: "BuildingType",
                column: "BuildingCostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resource_BuildingCost_BuildingCostId",
                table: "Resource",
                column: "BuildingCostId",
                principalTable: "BuildingCost",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resource_BuildingCost_BuildingCostId",
                table: "Resource");

            migrationBuilder.DropTable(
                name: "Building");

            migrationBuilder.DropTable(
                name: "BuildingType");

            migrationBuilder.DropTable(
                name: "BuildingCost");

            migrationBuilder.DropIndex(
                name: "IX_Resource_BuildingCostId",
                table: "Resource");

            migrationBuilder.DropColumn(
                name: "BuildingCostId",
                table: "Resource");

            migrationBuilder.CreateTable(
                name: "OpenAIRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessagesJson = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    ReplyMessage = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenAIRequest", x => x.Id);
                });
        }
    }
}
