using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInfoController.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IP",
                table: "LastHitByUser");

            migrationBuilder.RenameColumn(
                name: "UserIP",
                table: "AllowedAppsAndURLs",
                newName: "User");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "User",
                table: "AllowedAppsAndURLs",
                newName: "UserIP");

            migrationBuilder.AddColumn<string>(
                name: "IP",
                table: "LastHitByUser",
                type: "TEXT",
                nullable: true);
        }
    }
}
