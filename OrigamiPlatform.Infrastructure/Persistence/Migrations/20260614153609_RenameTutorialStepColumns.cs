using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTutorialStepColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "TutorialSteps");

            migrationBuilder.RenameColumn(
                name: "MediaUrl",
                table: "TutorialSteps",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "TutorialSteps",
                newName: "Description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "TutorialSteps",
                newName: "MediaUrl");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "TutorialSteps",
                newName: "Content");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "TutorialSteps",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }
    }
}
