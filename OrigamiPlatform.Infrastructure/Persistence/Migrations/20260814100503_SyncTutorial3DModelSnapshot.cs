using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncTutorial3DModelSnapshot : Migration
    {
        // No-op: this migration was scaffolded as a byte-for-byte duplicate of
        // 20260814082650_DropTutorial3DModelMetadata, which already dropped these two columns.
        // Running its original Up() a second time fails with "column does not exist" on any DB
        // that applies migrations in order — local, CI, or production alike. Both Up and Down are
        // emptied together so migrating past or below this point doesn't re-add or re-drop columns
        // that this migration never actually owned.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
