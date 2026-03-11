using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_OrderNo",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrderNo",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Products_BarcodeEan13",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Code",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_QrCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_VoucherNo",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PurchaseOrderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Companies_Code",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccounts_Code",
                table: "ChartOfAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_CashAccountId_TransactionDateUtc",
                table: "CashTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashAccounts_Code",
                table: "CashAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CariDebtItems_CariAccountId_TransactionDate",
                table: "CariDebtItems");

            migrationBuilder.DropIndex(
                name: "IX_CariAccounts_Code",
                table: "CariAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Branches_Code",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_BankAccountId_TransactionDateUtc",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_Iban",
                table: "BankAccounts");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "SalesOrderItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "PurchaseOrderItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "JournalEntryLines",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "InvoiceItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "FinanceMovements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "ChartOfAccounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "CashTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "CashAccounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "CariDebtItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "CariAccounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "Branches",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "BankTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantAccountId",
                table: "BankAccounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                DECLARE @DefaultTenantId uniqueidentifier;
                SELECT @DefaultTenantId = [Id]
                FROM [TenantAccounts]
                WHERE [Code] = N'dev-retail' AND [IsDeleted] = 0;

                IF @DefaultTenantId IS NULL
                BEGIN
                    SET @DefaultTenantId = '11111111-1111-1111-1111-111111111111';

                    IF NOT EXISTS (SELECT 1 FROM [TenantAccounts] WHERE [Id] = @DefaultTenantId)
                    BEGIN
                        INSERT INTO [TenantAccounts]
                        (
                            [Id], [Name], [Code], [Plan], [SubscriptionStatus],
                            [SubscriptionStartAtUtc], [SubscriptionEndAtUtc], [MaxUsers],
                            [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted], [DeletedAtUtc]
                        )
                        VALUES
                        (
                            @DefaultTenantId, N'Dev Retail', N'dev-retail', 3, 1,
                            SYSUTCDATETIME(), DATEADD(month, 1, SYSUTCDATETIME()), 50,
                            SYSUTCDATETIME(), NULL, 0, NULL
                        );
                    END
                END

                UPDATE [Warehouses] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [StockMovements] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [SalesOrders] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [SalesOrderItems] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [PurchaseOrders] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [PurchaseOrderItems] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [Products] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [JournalEntryLines] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [JournalEntries] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [Invoices] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [InvoiceItems] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [FinanceMovements] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [Companies] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [ChartOfAccounts] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [CashTransactions] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [CashAccounts] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [CariDebtItems] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [CariAccounts] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [Branches] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [BankTransactions] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [BankAccounts] SET [TenantAccountId] = @DefaultTenantId WHERE [TenantAccountId] = '00000000-0000-0000-0000-000000000000';
                """);
            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantAccountId",
                table: "Warehouses",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantAccountId_Code",
                table: "Warehouses",
                columns: new[] { "TenantAccountId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantAccountId",
                table: "StockMovements",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantAccountId_WarehouseId_ProductId_MovementDateUtc",
                table: "StockMovements",
                columns: new[] { "TenantAccountId", "WarehouseId", "ProductId", "MovementDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TenantAccountId",
                table: "SalesOrders",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TenantAccountId_OrderNo",
                table: "SalesOrders",
                columns: new[] { "TenantAccountId", "OrderNo" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_TenantAccountId",
                table: "SalesOrderItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantAccountId",
                table: "PurchaseOrders",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantAccountId_OrderNo",
                table: "PurchaseOrders",
                columns: new[] { "TenantAccountId", "OrderNo" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_TenantAccountId",
                table: "PurchaseOrderItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantAccountId",
                table: "Products",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantAccountId_BarcodeEan13",
                table: "Products",
                columns: new[] { "TenantAccountId", "BarcodeEan13" },
                unique: true,
                filter: "[BarcodeEan13] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantAccountId_Code",
                table: "Products",
                columns: new[] { "TenantAccountId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantAccountId_QrCode",
                table: "Products",
                columns: new[] { "TenantAccountId", "QrCode" },
                unique: true,
                filter: "[QrCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_TenantAccountId",
                table: "JournalEntryLines",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantAccountId",
                table: "JournalEntries",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantAccountId_VoucherNo",
                table: "JournalEntries",
                columns: new[] { "TenantAccountId", "VoucherNo" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PurchaseOrderId",
                table: "Invoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantAccountId",
                table: "Invoices",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantAccountId_PurchaseOrderId",
                table: "Invoices",
                columns: new[] { "TenantAccountId", "PurchaseOrderId" },
                unique: true,
                filter: "[PurchaseOrderId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantAccountId_SalesOrderId",
                table: "Invoices",
                columns: new[] { "TenantAccountId", "SalesOrderId" },
                unique: true,
                filter: "[SalesOrderId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_TenantAccountId",
                table: "InvoiceItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceMovements_TenantAccountId",
                table: "FinanceMovements",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceMovements_TenantAccountId_CariAccountId_MovementDateUtc",
                table: "FinanceMovements",
                columns: new[] { "TenantAccountId", "CariAccountId", "MovementDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantAccountId",
                table: "Companies",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantAccountId_Code",
                table: "Companies",
                columns: new[] { "TenantAccountId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_TenantAccountId",
                table: "ChartOfAccounts",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_TenantAccountId_Code",
                table: "ChartOfAccounts",
                columns: new[] { "TenantAccountId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CashAccountId",
                table: "CashTransactions",
                column: "CashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_TenantAccountId",
                table: "CashTransactions",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_TenantAccountId_CashAccountId_TransactionDateUtc",
                table: "CashTransactions",
                columns: new[] { "TenantAccountId", "CashAccountId", "TransactionDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccounts_TenantAccountId",
                table: "CashAccounts",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashAccounts_TenantAccountId_Code",
                table: "CashAccounts",
                columns: new[] { "TenantAccountId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CariDebtItems_CariAccountId",
                table: "CariDebtItems",
                column: "CariAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CariDebtItems_TenantAccountId",
                table: "CariDebtItems",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CariDebtItems_TenantAccountId_CariAccountId_TransactionDate",
                table: "CariDebtItems",
                columns: new[] { "TenantAccountId", "CariAccountId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CariAccounts_TenantAccountId",
                table: "CariAccounts",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CariAccounts_TenantAccountId_Code",
                table: "CariAccounts",
                columns: new[] { "TenantAccountId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantAccountId",
                table: "Branches",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantAccountId_Code",
                table: "Branches",
                columns: new[] { "TenantAccountId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_BankAccountId",
                table: "BankTransactions",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_TenantAccountId",
                table: "BankTransactions",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_TenantAccountId_BankAccountId_TransactionDateUtc",
                table: "BankTransactions",
                columns: new[] { "TenantAccountId", "BankAccountId", "TransactionDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_TenantAccountId",
                table: "BankAccounts",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_TenantAccountId_Iban",
                table: "BankAccounts",
                columns: new[] { "TenantAccountId", "Iban" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_TenantAccounts_TenantAccountId",
                table: "BankAccounts",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankTransactions_TenantAccounts_TenantAccountId",
                table: "BankTransactions",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_TenantAccounts_TenantAccountId",
                table: "Branches",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CariAccounts_TenantAccounts_TenantAccountId",
                table: "CariAccounts",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CariDebtItems_TenantAccounts_TenantAccountId",
                table: "CariDebtItems",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashAccounts_TenantAccounts_TenantAccountId",
                table: "CashAccounts",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransactions_TenantAccounts_TenantAccountId",
                table: "CashTransactions",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChartOfAccounts_TenantAccounts_TenantAccountId",
                table: "ChartOfAccounts",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_TenantAccounts_TenantAccountId",
                table: "Companies",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceMovements_TenantAccounts_TenantAccountId",
                table: "FinanceMovements",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_TenantAccounts_TenantAccountId",
                table: "InvoiceItems",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TenantAccounts_TenantAccountId",
                table: "Invoices",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_TenantAccounts_TenantAccountId",
                table: "JournalEntries",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_TenantAccounts_TenantAccountId",
                table: "JournalEntryLines",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TenantAccounts_TenantAccountId",
                table: "Products",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_TenantAccounts_TenantAccountId",
                table: "PurchaseOrderItems",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_TenantAccounts_TenantAccountId",
                table: "PurchaseOrders",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderItems_TenantAccounts_TenantAccountId",
                table: "SalesOrderItems",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_TenantAccounts_TenantAccountId",
                table: "SalesOrders",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_TenantAccounts_TenantAccountId",
                table: "StockMovements",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_TenantAccounts_TenantAccountId",
                table: "Warehouses",
                column: "TenantAccountId",
                principalTable: "TenantAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccounts_TenantAccounts_TenantAccountId",
                table: "BankAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_BankTransactions_TenantAccounts_TenantAccountId",
                table: "BankTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Branches_TenantAccounts_TenantAccountId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_CariAccounts_TenantAccounts_TenantAccountId",
                table: "CariAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_CariDebtItems_TenantAccounts_TenantAccountId",
                table: "CariDebtItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CashAccounts_TenantAccounts_TenantAccountId",
                table: "CashAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_CashTransactions_TenantAccounts_TenantAccountId",
                table: "CashTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ChartOfAccounts_TenantAccounts_TenantAccountId",
                table: "ChartOfAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_TenantAccounts_TenantAccountId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceMovements_TenantAccounts_TenantAccountId",
                table: "FinanceMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_TenantAccounts_TenantAccountId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TenantAccounts_TenantAccountId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_TenantAccounts_TenantAccountId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_TenantAccounts_TenantAccountId",
                table: "JournalEntryLines");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_TenantAccounts_TenantAccountId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_TenantAccounts_TenantAccountId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_TenantAccounts_TenantAccountId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderItems_TenantAccounts_TenantAccountId",
                table: "SalesOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_TenantAccounts_TenantAccountId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_TenantAccounts_TenantAccountId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_TenantAccounts_TenantAccountId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_TenantAccountId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_TenantAccountId_Code",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantAccountId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantAccountId_WarehouseId_ProductId_MovementDateUtc",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TenantAccountId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TenantAccountId_OrderNo",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderItems_TenantAccountId",
                table: "SalesOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantAccountId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantAccountId_OrderNo",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_TenantAccountId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantAccountId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantAccountId_BarcodeEan13",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantAccountId_Code",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantAccountId_QrCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_TenantAccountId",
                table: "JournalEntryLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_TenantAccountId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_TenantAccountId_VoucherNo",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PurchaseOrderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantAccountId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantAccountId_PurchaseOrderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantAccountId_SalesOrderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_TenantAccountId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_FinanceMovements_TenantAccountId",
                table: "FinanceMovements");

            migrationBuilder.DropIndex(
                name: "IX_FinanceMovements_TenantAccountId_CariAccountId_MovementDateUtc",
                table: "FinanceMovements");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TenantAccountId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TenantAccountId_Code",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccounts_TenantAccountId",
                table: "ChartOfAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccounts_TenantAccountId_Code",
                table: "ChartOfAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_CashAccountId",
                table: "CashTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_TenantAccountId",
                table: "CashTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_TenantAccountId_CashAccountId_TransactionDateUtc",
                table: "CashTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashAccounts_TenantAccountId",
                table: "CashAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CashAccounts_TenantAccountId_Code",
                table: "CashAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CariDebtItems_CariAccountId",
                table: "CariDebtItems");

            migrationBuilder.DropIndex(
                name: "IX_CariDebtItems_TenantAccountId",
                table: "CariDebtItems");

            migrationBuilder.DropIndex(
                name: "IX_CariDebtItems_TenantAccountId_CariAccountId_TransactionDate",
                table: "CariDebtItems");

            migrationBuilder.DropIndex(
                name: "IX_CariAccounts_TenantAccountId",
                table: "CariAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CariAccounts_TenantAccountId_Code",
                table: "CariAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Branches_TenantAccountId",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Branches_TenantAccountId_Code",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_BankAccountId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_TenantAccountId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_TenantAccountId_BankAccountId_TransactionDateUtc",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_TenantAccountId",
                table: "BankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_TenantAccountId_Iban",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "SalesOrderItems");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "FinanceMovements");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "ChartOfAccounts");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "CashAccounts");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "CariDebtItems");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "CariAccounts");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "TenantAccountId",
                table: "BankAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OrderNo",
                table: "SalesOrders",
                column: "OrderNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNo",
                table: "PurchaseOrders",
                column: "OrderNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BarcodeEan13",
                table: "Products",
                column: "BarcodeEan13",
                unique: true,
                filter: "[BarcodeEan13] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_QrCode",
                table: "Products",
                column: "QrCode",
                unique: true,
                filter: "[QrCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_VoucherNo",
                table: "JournalEntries",
                column: "VoucherNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PurchaseOrderId",
                table: "Invoices",
                column: "PurchaseOrderId",
                unique: true,
                filter: "[PurchaseOrderId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices",
                column: "SalesOrderId",
                unique: true,
                filter: "[SalesOrderId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                table: "Companies",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_Code",
                table: "ChartOfAccounts",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CashAccountId_TransactionDateUtc",
                table: "CashTransactions",
                columns: new[] { "CashAccountId", "TransactionDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccounts_Code",
                table: "CashAccounts",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CariDebtItems_CariAccountId_TransactionDate",
                table: "CariDebtItems",
                columns: new[] { "CariAccountId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CariAccounts_Code",
                table: "CariAccounts",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_BankAccountId_TransactionDateUtc",
                table: "BankTransactions",
                columns: new[] { "BankAccountId", "TransactionDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_Iban",
                table: "BankAccounts",
                column: "Iban",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}

