using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCountSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InventoryCountSessionId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryCountSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LocationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedByUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedItems = table.Column<int>(type: "int", nullable: false),
                    AppliedItems = table.Column<int>(type: "int", nullable: false),
                    SkippedItems = table.Column<int>(type: "int", nullable: false),
                    TotalIncreaseQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TotalDecreaseQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountSessions_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountSessions_Users_StartedByUserId",
                        column: x => x.StartedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryCountSessions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountSessionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryCountSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    DifferenceQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CountedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountSessionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountSessionItems_InventoryCountSessions_InventoryCountSessionId",
                        column: x => x.InventoryCountSessionId,
                        principalTable: "InventoryCountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCountSessionItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCountSessionItems_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryCountSessionId",
                table: "StockMovements",
                column: "InventoryCountSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessionItems_InventoryCountSessionId",
                table: "InventoryCountSessionItems",
                column: "InventoryCountSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessionItems_ProductId",
                table: "InventoryCountSessionItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessionItems_TenantAccountId",
                table: "InventoryCountSessionItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessionItems_TenantAccountId_InventoryCountSessionId",
                table: "InventoryCountSessionItems",
                columns: new[] { "TenantAccountId", "InventoryCountSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessionItems_TenantAccountId_ProductId_CountedAtUtc",
                table: "InventoryCountSessionItems",
                columns: new[] { "TenantAccountId", "ProductId", "CountedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessions_StartedByUserId",
                table: "InventoryCountSessions",
                column: "StartedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessions_TenantAccountId",
                table: "InventoryCountSessions",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessions_TenantAccountId_Status_StartedAtUtc",
                table: "InventoryCountSessions",
                columns: new[] { "TenantAccountId", "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessions_TenantAccountId_WarehouseId_StartedAtUtc",
                table: "InventoryCountSessions",
                columns: new[] { "TenantAccountId", "WarehouseId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountSessions_WarehouseId",
                table: "InventoryCountSessions",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_InventoryCountSessions_InventoryCountSessionId",
                table: "StockMovements",
                column: "InventoryCountSessionId",
                principalTable: "InventoryCountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_InventoryCountSessions_InventoryCountSessionId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "InventoryCountSessionItems");

            migrationBuilder.DropTable(
                name: "InventoryCountSessions");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_InventoryCountSessionId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "InventoryCountSessionId",
                table: "StockMovements");
        }
    }
}
