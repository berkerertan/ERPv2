using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderApprovalAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "SalesOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserName",
                table: "SalesOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "SalesOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "SalesOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserName",
                table: "SalesOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserName",
                table: "PurchaseOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "PurchaseOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserName",
                table: "PurchaseOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ApprovedByUserId",
                table: "SalesOrders",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CancelledByUserId",
                table: "SalesOrders",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ApprovedByUserId",
                table: "PurchaseOrders",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CancelledByUserId",
                table: "PurchaseOrders",
                column: "CancelledByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_ApprovedByUserId",
                table: "PurchaseOrders",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_CancelledByUserId",
                table: "PurchaseOrders",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Users_ApprovedByUserId",
                table: "SalesOrders",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Users_CancelledByUserId",
                table: "SalesOrders",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_ApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_CancelledByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Users_ApprovedByUserId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Users_CancelledByUserId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_ApprovedByUserId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CancelledByUserId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CancelledByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserName",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserName",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserName",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserName",
                table: "PurchaseOrders");
        }
    }
}
