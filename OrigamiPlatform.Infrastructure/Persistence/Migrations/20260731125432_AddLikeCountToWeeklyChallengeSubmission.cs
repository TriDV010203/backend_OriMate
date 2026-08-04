using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLikeCountToWeeklyChallengeSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "WeeklyChallengeSubmissions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "WeeklyChallengeSubmissions");
        }
    }
}
