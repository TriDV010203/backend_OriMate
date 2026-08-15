using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropDailyQuestEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDailyQuestProgresses");

            migrationBuilder.DropTable(
                name: "DailyQuests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyQuests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TargetValue = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyQuests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDailyQuestProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    QuestDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDailyQuestProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDailyQuestProgresses_DailyQuests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "DailyQuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDailyQuestProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyQuestProgresses_QuestId",
                table: "UserDailyQuestProgresses",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyQuestProgresses_UserId_QuestId_QuestDate",
                table: "UserDailyQuestProgresses",
                columns: new[] { "UserId", "QuestId", "QuestDate" },
                unique: true);
        }
    }
}
