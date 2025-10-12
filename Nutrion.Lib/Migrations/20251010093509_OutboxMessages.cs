using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrion.Data.Migrations
{
    /// <inheritdoc />
    public partial class OutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "OutboxMessage");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "OutboxMessage",
                newName: "Payload");

            migrationBuilder.AddColumn<string>(
                name: "AggregateId",
                table: "OutboxMessage",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AggregateType",
                table: "OutboxMessage",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "OutboxMessage",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "OutboxMessage",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ProcessedOn_Topic",
                table: "OutboxMessage",
                columns: new[] { "ProcessedOn", "Topic" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ProcessedOn_Topic",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "AggregateId",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "AggregateType",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "OutboxMessage");

            migrationBuilder.RenameColumn(
                name: "Payload",
                table: "OutboxMessage",
                newName: "Type");

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "OutboxMessage",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
