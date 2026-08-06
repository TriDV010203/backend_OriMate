using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningPathModes : Migration
    {
        // Fixed id for the default "Cơ bản" mode created to backfill existing (pre-mode) LearningPath rows.
        private static readonly Guid DefaultModeId = new("00000000-0000-0000-0000-000000000001");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningPathModes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPathModes", x => x.Id);
                });

            // Seed the default "Cơ bản" mode BEFORE adding the (NOT NULL) LearningPathModeId column below,
            // so the column's DEFAULT backfills every existing LearningPath row with a valid mode id.
            migrationBuilder.InsertData(
                table: "LearningPathModes",
                columns: new[] { "Id", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { DefaultModeId, "Cơ bản", "Chế độ khởi đầu, luôn mở cho mọi người.", 1, true, new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.AddColumn<Guid>(
                name: "LearningPathModeId",
                table: "LearningPaths",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: DefaultModeId);

            migrationBuilder.CreateTable(
                name: "LearningPathModeUnlockTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningPathModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TutorialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPathModeUnlockTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningPathModeUnlockTests_LearningPathModes_LearningPathModeId",
                        column: x => x.LearningPathModeId,
                        principalTable: "LearningPathModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningPathModeUnlockTests_Tutorials_TutorialId",
                        column: x => x.TutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModeUnlockSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningPathModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TutorialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModeUnlockSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModeUnlockSubmissions_LearningPathModes_LearningPathModeId",
                        column: x => x.LearningPathModeId,
                        principalTable: "LearningPathModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModeUnlockSubmissions_Tutorials_TutorialId",
                        column: x => x.TutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModeUnlockSubmissions_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModeUnlockSubmissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningPaths_LearningPathModeId",
                table: "LearningPaths",
                column: "LearningPathModeId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathModes_SortOrder",
                table: "LearningPathModes",
                column: "SortOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathModeUnlockTests_LearningPathModeId",
                table: "LearningPathModeUnlockTests",
                column: "LearningPathModeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathModeUnlockTests_TutorialId",
                table: "LearningPathModeUnlockTests",
                column: "TutorialId");

            migrationBuilder.CreateIndex(
                name: "IX_ModeUnlockSubmissions_LearningPathModeId",
                table: "ModeUnlockSubmissions",
                column: "LearningPathModeId");

            migrationBuilder.CreateIndex(
                name: "IX_ModeUnlockSubmissions_ReviewedByUserId",
                table: "ModeUnlockSubmissions",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModeUnlockSubmissions_TutorialId",
                table: "ModeUnlockSubmissions",
                column: "TutorialId");

            migrationBuilder.CreateIndex(
                name: "IX_ModeUnlockSubmissions_UserId_LearningPathModeId_Status",
                table: "ModeUnlockSubmissions",
                columns: new[] { "UserId", "LearningPathModeId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_LearningPaths_LearningPathModes_LearningPathModeId",
                table: "LearningPaths",
                column: "LearningPathModeId",
                principalTable: "LearningPathModes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningPaths_LearningPathModes_LearningPathModeId",
                table: "LearningPaths");

            migrationBuilder.DropTable(
                name: "LearningPathModeUnlockTests");

            migrationBuilder.DropTable(
                name: "ModeUnlockSubmissions");

            migrationBuilder.DropTable(
                name: "LearningPathModes");

            migrationBuilder.DropIndex(
                name: "IX_LearningPaths_LearningPathModeId",
                table: "LearningPaths");

            migrationBuilder.DropColumn(
                name: "LearningPathModeId",
                table: "LearningPaths");
        }
    }
}
