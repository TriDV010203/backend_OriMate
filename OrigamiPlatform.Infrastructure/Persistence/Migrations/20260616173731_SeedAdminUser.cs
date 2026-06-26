using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrigamiPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "RefreshTokenExpiresAt", "RefreshTokenHash", "Status", "TokenExpiry", "UpdatedAt", "VerificationToken" },
                values: new object[] { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@origami.com", "$2a$11$oJ4bYk4funN7ZPGnjXt0c.q3Or1/y/8TtCQjNMLkmDHNleEkPuGA6", null, null, null, null, "Active", null, null, null });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "UserId", "AvatarUrl", "Bio", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[] { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin", null });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Role", "UserId", "CreatedAt" },
                values: new object[,]
                {
                    { "Admin", new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "User", new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "UserId",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "Role", "UserId" },
                keyValues: new object[] { "Admin", new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") });

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "Role", "UserId" },
                keyValues: new object[] { "User", new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") });

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
        }
    }
}
