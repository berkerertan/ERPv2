using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCountClientRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "InventoryCountSessions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessions_TenantAccountId_ClientRequestId",
                table: "InventoryCountSessions",
                columns: new[] { "TenantAccountId", "ClientRequestId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [ClientRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryCountSessions_TenantAccountId_ClientRequestId",
                table: "InventoryCountSessions");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "InventoryCountSessions");
        }
    }
}
