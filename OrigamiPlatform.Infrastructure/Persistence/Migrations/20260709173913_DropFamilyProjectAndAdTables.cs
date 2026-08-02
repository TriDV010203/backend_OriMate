using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropFamilyProjectAndAdTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdClicks");

            migrationBuilder.DropTable(
                name: "AdImpressions");

            migrationBuilder.DropTable(
                name: "FamilyProjectMembers");

            migrationBuilder.DropTable(
                name: "FamilyProjectStepProgresses");

            migrationBuilder.DropTable(
                name: "AdBanners");

            migrationBuilder.DropTable(
                name: "FamilyProjects");

            migrationBuilder.DropTable(
                name: "AdCampaigns");

            migrationBuilder.DropTable(
                name: "FamilySubscriptions");

            migrationBuilder.DropTable(
                name: "AdPlacements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdPlacements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdPlacements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamilySubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilySubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilySubscriptions_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilySubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlacementId = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BudgetRemaining = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DestinationUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PricingType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RatePerUnit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdCampaigns_AdPlacements_PlacementId",
                        column: x => x.PlacementId,
                        principalTable: "AdPlacements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdCampaigns_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdCampaigns_Users_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FamilyProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TutorialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyProjects_FamilySubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "FamilySubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyProjects_Tutorials_TutorialId",
                        column: x => x.TutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyProjects_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdBanners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdBanners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdBanners_AdCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "AdCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyProjectMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvitedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyProjectMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyProjectMembers_FamilyProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "FamilyProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FamilyProjectStepProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyProjectStepProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyProjectStepProgresses_FamilyProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "FamilyProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyProjectStepProgresses_TutorialSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "TutorialSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyProjectStepProgresses_Users_CompletedBy",
                        column: x => x.CompletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdClicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdClicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdClicks_AdBanners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "AdBanners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdClicks_AdCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "AdCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdClicks_TutorialSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "TutorialSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdClicks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdImpressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdImpressions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdImpressions_AdBanners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "AdBanners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdImpressions_AdCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "AdCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdImpressions_TutorialSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "TutorialSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdImpressions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdBanners_CampaignId",
                table: "AdBanners",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_ApprovedBy",
                table: "AdCampaigns",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_PartnerId",
                table: "AdCampaigns",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_PlacementId",
                table: "AdCampaigns",
                column: "PlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_AdClicks_BannerId",
                table: "AdClicks",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdClicks_CampaignId",
                table: "AdClicks",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_AdClicks_StepId",
                table: "AdClicks",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_AdClicks_UserId",
                table: "AdClicks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdImpressions_BannerId",
                table: "AdImpressions",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdImpressions_CampaignId",
                table: "AdImpressions",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_AdImpressions_StepId",
                table: "AdImpressions",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_AdImpressions_UserId",
                table: "AdImpressions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyProjectMembers_ProjectId",
                table: "FamilyProjectMembers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyProjectMembers_UserId",
                table: "FamilyProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyProjects_OwnerId",
                table: "FamilyProjects",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyProjects_SubscriptionId",
                table: "FamilyProjects",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyProjects_TutorialId",
                table: "FamilyProjects",
                column: "TutorialId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyProjectStepProgresses_CompletedBy",
                table: "FamilyProjectStepProgresses",
                column: "CompletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyProjectStepProgresses_ProjectId",
                table: "FamilyProjectStepProgresses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyProjectStepProgresses_StepId",
                table: "FamilyProjectStepProgresses",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilySubscriptions_TransactionId",
                table: "FamilySubscriptions",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilySubscriptions_UserId",
                table: "FamilySubscriptions",
                column: "UserId");
        }
    }
}
