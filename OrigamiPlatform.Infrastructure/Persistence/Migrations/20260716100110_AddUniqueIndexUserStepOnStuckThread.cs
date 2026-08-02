using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexUserStepOnStuckThread : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StuckThreads_UserId",
                table: "StuckThreads");

            migrationBuilder.CreateIndex(
                name: "IX_StuckThreads_UserId_StepId",
                table: "StuckThreads",
                columns: new[] { "UserId", "StepId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StuckThreads_UserId_StepId",
                table: "StuckThreads");

            migrationBuilder.CreateIndex(
                name: "IX_StuckThreads_UserId",
                table: "StuckThreads",
                column: "UserId");
        }
    }
}
