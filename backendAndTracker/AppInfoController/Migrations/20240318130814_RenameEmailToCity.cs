using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInfoController.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmailToCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmailId",
                table: "LastHitByUser",
                newName: "City");
            migrationBuilder.InsertData("AppSettings", new string[2] { "Name", "Value" }, new object[] { "UserValidity", "10"});
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "City",
                table: "LastHitByUser",
                newName: "EmailId");
        }
    }
}
