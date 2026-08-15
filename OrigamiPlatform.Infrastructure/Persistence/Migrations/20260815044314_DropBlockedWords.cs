using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropBlockedWords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedWords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedWords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedWords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedWords_Word",
                table: "BlockedWords",
                column: "Word",
                unique: true);
        }
    }
}
