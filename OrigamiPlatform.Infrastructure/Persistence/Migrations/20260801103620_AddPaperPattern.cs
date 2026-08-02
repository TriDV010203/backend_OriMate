using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaperPattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaperPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PriceInHatGap = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaperPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPaperPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaperPatternId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPaperPatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPaperPatterns_PaperPatterns_PaperPatternId",
                        column: x => x.PaperPatternId,
                        principalTable: "PaperPatterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPaperPatterns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPaperPatterns_PaperPatternId",
                table: "UserPaperPatterns",
                column: "PaperPatternId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPaperPatterns_UserId_PaperPatternId",
                table: "UserPaperPatterns",
                columns: new[] { "UserId", "PaperPatternId" },
                unique: true);

            // Seed: 3 sample patterns so the shop has content to demo.
            migrationBuilder.InsertData(
                table: "PaperPatterns",
                columns: new[] { "Id", "Name", "Description", "ImageUrl", "PriceInHatGap", "IsActive", "CreatedAt" },
                values: new object[,]
                {
                    { new Guid("b1e1a001-0001-4000-8000-000000000001"), "Giấy Kim Tuyến", "Giấy gấp lấp lánh ánh kim, nổi bật cho mọi tác phẩm.", null, 50, true, new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b1e1a001-0001-4000-8000-000000000002"), "Giấy Hoa Văn Nhật Bản", "Hoạ tiết Washi truyền thống, phù hợp origami cổ điển.", null, 80, true, new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b1e1a001-0001-4000-8000-000000000003"), "Giấy Metalic Vàng", "Bề mặt ánh kim vàng sang trọng, giới hạn.", null, 120, true, new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPaperPatterns");

            migrationBuilder.DropTable(
                name: "PaperPatterns");
        }
    }
}
