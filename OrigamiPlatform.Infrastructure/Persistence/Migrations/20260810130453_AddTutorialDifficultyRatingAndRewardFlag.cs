using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorialDifficultyRatingAndRewardFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasBeenRewarded",
                table: "TutorialStepProgresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TutorialStepProgresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TutorialDifficultyRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TutorialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorialDifficultyRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorialDifficultyRatings_Tutorials_TutorialId",
                        column: x => x.TutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TutorialDifficultyRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TutorialDifficultyRatings_TutorialId",
                table: "TutorialDifficultyRatings",
                column: "TutorialId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorialDifficultyRatings_UserId_TutorialId",
                table: "TutorialDifficultyRatings",
                columns: new[] { "UserId", "TutorialId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TutorialDifficultyRatings");

            migrationBuilder.DropColumn(
                name: "HasBeenRewarded",
                table: "TutorialStepProgresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TutorialStepProgresses");
        }
    }
}
