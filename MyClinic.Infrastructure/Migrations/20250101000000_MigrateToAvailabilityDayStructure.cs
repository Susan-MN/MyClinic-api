using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToAvailabilityDayStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create AvailabilityDays table
            migrationBuilder.CreateTable(
                name: "AvailabilityDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SlotDuration = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilityDays_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.UniqueConstraint("AK_AvailabilityDays_DoctorId_DayOfWeek", x => new { x.DoctorId, x.DayOfWeek });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityDays_DoctorId",
                table: "AvailabilityDays",
                column: "DoctorId");

            // Create AvailabilityExceptions table
            migrationBuilder.CreateTable(
                name: "AvailabilityExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    ExceptionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    CustomStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CustomEndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilityExceptions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.UniqueConstraint("AK_AvailabilityExceptions_DoctorId_ExceptionDate", x => new { x.DoctorId, x.ExceptionDate });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityExceptions_DoctorId",
                table: "AvailabilityExceptions",
                column: "DoctorId");

            // Migrate data from old Availabilities table to AvailabilityDays
            migrationBuilder.Sql(@"
                INSERT INTO AvailabilityDays (DoctorId, DayOfWeek, StartTime, EndTime, SlotDuration, IsActive)
                SELECT 
                    a.DoctorId,
                    CASE 
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%SUNDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%sunday%' THEN 0
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%MONDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%monday%' THEN 1
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%TUESDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%tuesday%' THEN 2
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%WEDNESDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%wednesday%' THEN 3
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%THURSDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%thursday%' THEN 4
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%FRIDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%friday%' THEN 5
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%SATURDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%saturday%' THEN 6
                        ELSE -1
                    END,
                    CAST(a.StartTime AS TIME),
                    CAST(a.EndTime AS TIME),
                    a.SlotDuration,
                    a.IsActive
                FROM Availabilities a
                WHERE a.WorkingDaysJson IS NOT NULL 
                    AND a.WorkingDaysJson != '[]'
                    AND a.WorkingDaysJson != 'null'
                    AND JSON_VALUE(a.WorkingDaysJson, '$[0]') IS NOT NULL
                    AND CASE 
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%SUNDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%sunday%' THEN 0
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%MONDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%monday%' THEN 1
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%TUESDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%tuesday%' THEN 2
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%WEDNESDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%wednesday%' THEN 3
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%THURSDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%thursday%' THEN 4
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%FRIDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%friday%' THEN 5
                        WHEN JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%SATURDAY%' OR JSON_VALUE(a.WorkingDaysJson, '$[0]') LIKE '%saturday%' THEN 6
                        ELSE -1
                    END >= 0
            ");

            // Migrate data from Leaves table to AvailabilityExceptions
            migrationBuilder.Sql(@"
                INSERT INTO AvailabilityExceptions (DoctorId, ExceptionDate, IsAvailable, CustomStartTime, CustomEndTime, Reason, Type)
                SELECT 
                    DoctorId,
                    StartDate,
                    0, -- IsAvailable = false (on leave)
                    NULL, -- No custom hours for leaves
                    NULL,
                    Reason,
                    0 -- Type = Leave
                FROM Leaves
                WHERE IsApproved = 1
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityExceptions");

            migrationBuilder.DropTable(
                name: "AvailabilityDays");
        }
    }
}





