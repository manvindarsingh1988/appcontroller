using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppInfoController.Migrations
{
    /// <inheritdoc />
    public partial class AddCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("AppSettings", new string[2] { "Name", "Value" }, new object[] { "UserName", "saral" });
            migrationBuilder.InsertData("AppSettings", new string[2] { "Name", "Value" }, new object[] { "Password", "Saral@2018" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
