using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflecta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPinLockSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "biometric_enabled",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "lock_timeout_minutes",
                table: "user_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "biometric_enabled",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "lock_timeout_minutes",
                table: "user_settings");
        }
    }
}
