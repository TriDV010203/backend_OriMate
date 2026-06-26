using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorialStepProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TutorialStepProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TutorialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TutorialStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorialStepProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorialStepProgresses_TutorialSteps_TutorialStepId",
                        column: x => x.TutorialStepId,
                        principalTable: "TutorialSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TutorialStepProgresses_Tutorials_TutorialId",
                        column: x => x.TutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TutorialStepProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TutorialStepProgresses_TutorialId",
                table: "TutorialStepProgresses",
                column: "TutorialId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorialStepProgresses_TutorialStepId",
                table: "TutorialStepProgresses",
                column: "TutorialStepId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorialStepProgresses_UserId_TutorialStepId",
                table: "TutorialStepProgresses",
                columns: new[] { "UserId", "TutorialStepId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TutorialStepProgresses");
        }
    }
}
