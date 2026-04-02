using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementProofFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProofImagePublicId",
                table: "StockMovements",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofImageUrl",
                table: "StockMovements",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonNote",
                table: "StockMovements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofImagePublicId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ProofImageUrl",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ReasonNote",
                table: "StockMovements");
        }
    }
}
