using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorProfileComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProfileComplete",
                table: "Doctors",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileComplete",
                table: "Doctors");
        }
    }
}
