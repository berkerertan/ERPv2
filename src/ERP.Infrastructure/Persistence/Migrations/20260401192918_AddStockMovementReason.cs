using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Reason",
                table: "StockMovements",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE [StockMovements] SET [Reason] = 1 WHERE [Reason] = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "StockMovements");
        }
    }
}
