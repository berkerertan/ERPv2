using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosBarcodeAndQuickSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BarcodeEan13",
                table: "Products",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultSalePrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Products",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BarcodeEan13",
                table: "Products",
                column: "BarcodeEan13",
                unique: true,
                filter: "[BarcodeEan13] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_QrCode",
                table: "Products",
                column: "QrCode",
                unique: true,
                filter: "[QrCode] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_BarcodeEan13",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_QrCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BarcodeEan13",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DefaultSalePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Products");
        }
    }
}
