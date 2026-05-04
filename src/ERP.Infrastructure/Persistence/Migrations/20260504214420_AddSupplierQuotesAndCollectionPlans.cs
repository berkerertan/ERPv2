using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierQuotesAndCollectionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionPlanEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CariAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    OverdueAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OverdueDays = table.Column<int>(type: "int", nullable: false),
                    RiskUsageRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NextActionDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PromiseDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedToUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    LastContactAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastContactNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionPlanEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionPlanEntries_CariAccounts_CariAccountId",
                        column: x => x.CariAccountId,
                        principalTable: "CariAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionPlanEntries_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuoteOfferItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierQuoteOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumOrderQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuoteOfferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteOfferItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteOfferItems_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuoteOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierQuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierCariAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuoteOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteOffers_CariAccounts_SupplierCariAccountId",
                        column: x => x.SupplierCariAccountId,
                        principalTable: "CariAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteOffers_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuoteRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NeededByDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SelectedSupplierCariAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SelectedOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuoteRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteRequests_CariAccounts_SelectedSupplierCariAccountId",
                        column: x => x.SelectedSupplierCariAccountId,
                        principalTable: "CariAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteRequests_SupplierQuoteOffers_SelectedOfferId",
                        column: x => x.SelectedOfferId,
                        principalTable: "SupplierQuoteOffers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuoteRequests_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteRequests_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuoteRequestItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierQuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TargetUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuoteRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteRequestItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteRequestItems_SupplierQuoteRequests_SupplierQuoteRequestId",
                        column: x => x.SupplierQuoteRequestId,
                        principalTable: "SupplierQuoteRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteRequestItems_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPlanEntries_CariAccountId",
                table: "CollectionPlanEntries",
                column: "CariAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPlanEntries_TenantAccountId",
                table: "CollectionPlanEntries",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPlanEntries_TenantAccountId_CariAccountId_Status",
                table: "CollectionPlanEntries",
                columns: new[] { "TenantAccountId", "CariAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPlanEntries_TenantAccountId_Priority_NextActionDateUtc",
                table: "CollectionPlanEntries",
                columns: new[] { "TenantAccountId", "Priority", "NextActionDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteOfferItems_ProductId",
                table: "SupplierQuoteOfferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteOfferItems_SupplierQuoteOfferId",
                table: "SupplierQuoteOfferItems",
                column: "SupplierQuoteOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteOfferItems_TenantAccountId",
                table: "SupplierQuoteOfferItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteOfferItems_TenantAccountId_SupplierQuoteOfferId",
                table: "SupplierQuoteOfferItems",
                columns: new[] { "TenantAccountId", "SupplierQuoteOfferId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteOffers_SupplierCariAccountId",
                table: "SupplierQuoteOffers",
                column: "SupplierCariAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteOffers_SupplierQuoteRequestId",
                table: "SupplierQuoteOffers",
                column: "SupplierQuoteRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteOffers_TenantAccountId",
                table: "SupplierQuoteOffers",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteOffers_TenantAccountId_SupplierQuoteRequestId_SupplierCariAccountId",
                table: "SupplierQuoteOffers",
                columns: new[] { "TenantAccountId", "SupplierQuoteRequestId", "SupplierCariAccountId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequestItems_ProductId",
                table: "SupplierQuoteRequestItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequestItems_SupplierQuoteRequestId",
                table: "SupplierQuoteRequestItems",
                column: "SupplierQuoteRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequestItems_TenantAccountId",
                table: "SupplierQuoteRequestItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequestItems_TenantAccountId_SupplierQuoteRequestId",
                table: "SupplierQuoteRequestItems",
                columns: new[] { "TenantAccountId", "SupplierQuoteRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequests_SelectedOfferId",
                table: "SupplierQuoteRequests",
                column: "SelectedOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequests_SelectedSupplierCariAccountId",
                table: "SupplierQuoteRequests",
                column: "SelectedSupplierCariAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequests_TenantAccountId",
                table: "SupplierQuoteRequests",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequests_TenantAccountId_RequestNo",
                table: "SupplierQuoteRequests",
                columns: new[] { "TenantAccountId", "RequestNo" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequests_TenantAccountId_Status_CreatedAtUtc",
                table: "SupplierQuoteRequests",
                columns: new[] { "TenantAccountId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteRequests_WarehouseId",
                table: "SupplierQuoteRequests",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierQuoteOfferItems_SupplierQuoteOffers_SupplierQuoteOfferId",
                table: "SupplierQuoteOfferItems",
                column: "SupplierQuoteOfferId",
                principalTable: "SupplierQuoteOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierQuoteOffers_SupplierQuoteRequests_SupplierQuoteRequestId",
                table: "SupplierQuoteOffers",
                column: "SupplierQuoteRequestId",
                principalTable: "SupplierQuoteRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierQuoteRequests_SupplierQuoteOffers_SelectedOfferId",
                table: "SupplierQuoteRequests");

            migrationBuilder.DropTable(
                name: "CollectionPlanEntries");

            migrationBuilder.DropTable(
                name: "SupplierQuoteOfferItems");

            migrationBuilder.DropTable(
                name: "SupplierQuoteRequestItems");

            migrationBuilder.DropTable(
                name: "SupplierQuoteOffers");

            migrationBuilder.DropTable(
                name: "SupplierQuoteRequests");
        }
    }
}
