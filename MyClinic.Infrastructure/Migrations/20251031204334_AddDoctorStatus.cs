using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Doctors",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Doctors");
        }
    }
}
