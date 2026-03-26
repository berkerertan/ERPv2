using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformEmailCampaignQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "PlatformEmailDispatchLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlatformEmailCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TemplateKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    IsHtml = table.Column<bool>(type: "bit", nullable: false),
                    SendToAllActiveTenants = table.Column<bool>(type: "bit", nullable: false),
                    SendToAllTenantUsers = table.Column<bool>(type: "bit", nullable: false),
                    TenantIdsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    VariablesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalRecipients = table.Column<int>(type: "int", nullable: false),
                    SentCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformEmailCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformEmailCampaigns_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlatformEmailCampaignRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TenantName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAttemptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProviderMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformEmailCampaignRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformEmailCampaignRecipients_PlatformEmailCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "PlatformEmailCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatformEmailCampaignRecipients_TenantAccounts_TenantAccountId",
                        column: x => x.TenantAccountId,
                        principalTable: "TenantAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailDispatchLogs_CampaignId",
                table: "PlatformEmailDispatchLogs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaignRecipients_CampaignId",
                table: "PlatformEmailCampaignRecipients",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaignRecipients_CampaignId_RecipientEmail",
                table: "PlatformEmailCampaignRecipients",
                columns: new[] { "CampaignId", "RecipientEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaignRecipients_CampaignId_Status_NextAttemptAtUtc",
                table: "PlatformEmailCampaignRecipients",
                columns: new[] { "CampaignId", "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaignRecipients_CampaignId_TenantAccountId",
                table: "PlatformEmailCampaignRecipients",
                columns: new[] { "CampaignId", "TenantAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaignRecipients_TenantAccountId",
                table: "PlatformEmailCampaignRecipients",
                column: "TenantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaigns_CreatedAtUtc",
                table: "PlatformEmailCampaigns",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaigns_CreatedByUserId",
                table: "PlatformEmailCampaigns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaigns_ScheduledAtUtc",
                table: "PlatformEmailCampaigns",
                column: "ScheduledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformEmailCampaigns_Status",
                table: "PlatformEmailCampaigns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformEmailCampaignRecipients");

            migrationBuilder.DropTable(
                name: "PlatformEmailCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_PlatformEmailDispatchLogs_CampaignId",
                table: "PlatformEmailDispatchLogs");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "PlatformEmailDispatchLogs");
        }
    }
}
