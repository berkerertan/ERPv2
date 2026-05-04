using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseRecommendationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseRecommendationSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SupplierCariAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AnalysisDays = table.Column<int>(type: "int", nullable: false),
                    CoverageDays = table.Column<int>(type: "int", nullable: false),
                    MaxItems = table.Column<int>(type: "int", nullable: false),
                    CriticalOnly = table.Column<bool>(type: "bit", nullable: false),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    CriticalItems = table.Column<int>(type: "int", nullable: false),
                    TotalRecommendedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TotalEstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ItemsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 32000, nullable: false),
                    SupplierGroupsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRecommendationSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRecommendationSnapshots_CariAccounts_SupplierCariAccountId",
                        column: x => x.SupplierCariAccountId,
                        principalTable: "CariAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRecommendationSnapshots_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRecommendationSnapshots_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRecommendationSnapshots_SupplierCariAccountId",
                table: "PurchaseRecommendationSnapshots",
                column: "SupplierCariAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRecommendationSnapshots_TenantAccountId",
                table: "PurchaseRecommendationSnapshots",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRecommendationSnapshots_TenantAccountId_CreatedAtUtc",
                table: "PurchaseRecommendationSnapshots",
                columns: new[] { "TenantAccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRecommendationSnapshots_TenantAccountId_WarehouseId_CreatedAtUtc",
                table: "PurchaseRecommendationSnapshots",
                columns: new[] { "TenantAccountId", "WarehouseId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRecommendationSnapshots_WarehouseId",
                table: "PurchaseRecommendationSnapshots",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseRecommendationSnapshots");
        }
    }
}
