using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInfoController.Migrations
{
    /// <inheritdoc />
    public partial class AddAppVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("AppSettings", new string[2] { "Name", "Value" }, new object[] { "AppVersion", "1.0.0" });
            migrationBuilder.AddColumn<string>(
                name: "AppVersion",
                table: "LastHitByUser",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppVersion",
                table: "LastHitByUser");
        }
    }
}
