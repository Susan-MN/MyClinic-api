namespace MyClinic.Domain.Entities
{
    public class Availability
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

        // JSON array of working day names (e.g., ["monday","tuesday"])
        public string WorkingDaysJson { get; set; } = "[]";

        // Stored as HH:mm strings for simplicity
        public string StartTime { get; set; } = "09:00";
        public string EndTime { get; set; } = "17:00";

        public int SlotDuration { get; set; } = 30; // in minutes
        public bool IsActive { get; set; } = true;
    }
}

