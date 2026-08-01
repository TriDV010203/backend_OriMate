using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedOfficialTutorials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FT-32: dev DB already has real tutorial rows (not empty), so mark existing published
            // tutorials as official curated content via UPDATE instead of inserting fabricated rows.
            migrationBuilder.Sql(
                "UPDATE Tutorials SET IsOfficial = 1 WHERE Status = 'Published' AND IsOfficial = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Tutorials SET IsOfficial = 0 WHERE Status = 'Published' AND IsOfficial = 1;");
        }
    }
}
