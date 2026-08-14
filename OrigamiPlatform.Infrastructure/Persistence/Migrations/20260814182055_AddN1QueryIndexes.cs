using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddN1QueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_TargetType_TargetId",
                table: "Wishlists",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tutorials_Status_IsDeleted_PublishedAt",
                table: "Tutorials",
                columns: new[] { "Status", "IsDeleted", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_TargetType_TargetId",
                table: "Likes",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_IsVisible_IsDeleted_CreatedAt",
                table: "CommunityPosts",
                columns: new[] { "IsVisible", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TargetType_TargetId",
                table: "Comments",
                columns: new[] { "TargetType", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wishlists_TargetType_TargetId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Tutorials_Status_IsDeleted_PublishedAt",
                table: "Tutorials");

            migrationBuilder.DropIndex(
                name: "IX_Likes_TargetType_TargetId",
                table: "Likes");

            migrationBuilder.DropIndex(
                name: "IX_CommunityPosts_IsVisible_IsDeleted_CreatedAt",
                table: "CommunityPosts");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TargetType_TargetId",
                table: "Comments");
        }
    }
}
