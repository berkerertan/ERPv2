using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCountItemTraceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CountedByUserId",
                table: "InventoryCountSessionItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountedByUserName",
                table: "InventoryCountSessionItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationCode",
                table: "InventoryCountSessionItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountedByUserId",
                table: "InventoryCountSessionItems");

            migrationBuilder.DropColumn(
                name: "CountedByUserName",
                table: "InventoryCountSessionItems");

            migrationBuilder.DropColumn(
                name: "LocationCode",
                table: "InventoryCountSessionItems");
        }
    }
}
