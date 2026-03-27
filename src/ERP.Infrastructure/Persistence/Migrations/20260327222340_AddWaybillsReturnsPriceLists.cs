using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaybillsReturnsPriceLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceLists_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Returns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CariAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReturnDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Returns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Returns_CariAccounts_CariAccountId",
                        column: x => x.CariAccountId,
                        principalTable: "CariAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Returns_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Returns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Waybills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WaybillNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CariAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShipDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waybills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Waybills_CariAccounts_CariAccountId",
                        column: x => x.CariAccountId,
                        principalTable: "CariAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Waybills_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Waybills_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListItems_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceListItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceListItems_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnItems_Returns_ReturnId",
                        column: x => x.ReturnId,
                        principalTable: "Returns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnItems_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WaybillItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WaybillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaybillItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaybillItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaybillItems_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaybillItems_Waybills_WaybillId",
                        column: x => x.WaybillId,
                        principalTable: "Waybills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_PriceListId",
                table: "PriceListItems",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_ProductId",
                table: "PriceListItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_TenantAccountId",
                table: "PriceListItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_TenantAccountId",
                table: "PriceLists",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ProductId",
                table: "ReturnItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ReturnId",
                table: "ReturnItems",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_TenantAccountId",
                table: "ReturnItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_CariAccountId",
                table: "Returns",
                column: "CariAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantAccountId",
                table: "Returns",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_WarehouseId",
                table: "Returns",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_TenantAccountId",
                table: "UserNotifications",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WaybillItems_ProductId",
                table: "WaybillItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WaybillItems_TenantAccountId",
                table: "WaybillItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WaybillItems_WaybillId",
                table: "WaybillItems",
                column: "WaybillId");

            migrationBuilder.CreateIndex(
                name: "IX_Waybills_CariAccountId",
                table: "Waybills",
                column: "CariAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Waybills_TenantAccountId",
                table: "Waybills",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Waybills_WarehouseId",
                table: "Waybills",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceListItems");

            migrationBuilder.DropTable(
                name: "ReturnItems");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "WaybillItems");

            migrationBuilder.DropTable(
                name: "PriceLists");

            migrationBuilder.DropTable(
                name: "Returns");

            migrationBuilder.DropTable(
                name: "Waybills");
        }
    }
}
