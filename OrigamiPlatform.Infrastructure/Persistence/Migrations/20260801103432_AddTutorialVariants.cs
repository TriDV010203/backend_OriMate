using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorialVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TutorialVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentTutorialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantTutorialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DifficultyDelta = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorialVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorialVariants_Tutorials_ParentTutorialId",
                        column: x => x.ParentTutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TutorialVariants_Tutorials_VariantTutorialId",
                        column: x => x.VariantTutorialId,
                        principalTable: "Tutorials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TutorialVariants_ParentTutorialId_VariantTutorialId",
                table: "TutorialVariants",
                columns: new[] { "ParentTutorialId", "VariantTutorialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TutorialVariants_VariantTutorialId",
                table: "TutorialVariants",
                column: "VariantTutorialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TutorialVariants");
        }
    }
}
