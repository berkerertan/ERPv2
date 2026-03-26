using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformAdminEmailOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformEmailDispatchLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TenantName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TemplateKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggeredByUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformEmailDispatchLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformEmailDispatchLogs_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformEmailDispatchLogs_Users_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlatformEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformEmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailDispatchLogs_AttemptedAtUtc",
                table: "PlatformEmailDispatchLogs",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailDispatchLogs_Status",
                table: "PlatformEmailDispatchLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailDispatchLogs_TemplateKey",
                table: "PlatformEmailDispatchLogs",
                column: "TemplateKey");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailDispatchLogs_TenantAccountId",
                table: "PlatformEmailDispatchLogs",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailDispatchLogs_TriggeredByUserId",
                table: "PlatformEmailDispatchLogs",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailTemplates_Key",
                table: "PlatformEmailTemplates",
                column: "Key",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformEmailDispatchLogs");

            migrationBuilder.DropTable(
                name: "PlatformEmailTemplates");
        }
    }
}
