using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeycloakId",
                table: "Admins",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeycloakId",
                table: "Admins");
        }
    }
}
