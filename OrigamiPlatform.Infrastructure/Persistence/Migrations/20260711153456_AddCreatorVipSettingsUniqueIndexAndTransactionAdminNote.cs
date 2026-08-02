using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorVipSettingsUniqueIndexAndTransactionAdminNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreatorVipSettings_CreatorId",
                table: "CreatorVipSettings");

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Transactions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorVipSettings_CreatorId",
                table: "CreatorVipSettings",
                column: "CreatorId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreatorVipSettings_CreatorId",
                table: "CreatorVipSettings");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorVipSettings_CreatorId",
                table: "CreatorVipSettings",
                column: "CreatorId");
        }
    }
}
