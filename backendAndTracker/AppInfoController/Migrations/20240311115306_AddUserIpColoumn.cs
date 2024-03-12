using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInfoController.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIpColoumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserIP",
                table: "AllowedAppsAndURLs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserIP",
                table: "AllowedAppsAndURLs");
        }
    }
}
