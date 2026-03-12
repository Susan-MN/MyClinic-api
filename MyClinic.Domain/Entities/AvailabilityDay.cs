namespace MyClinic.Domain.Entities
{
    public class AvailabilityDay
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;
        
        // DayOfWeek: 0=Sunday, 1=Monday, ..., 6=Saturday
        public int DayOfWeek { get; set; }
        
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int SlotDuration { get; set; } // in minutes
        public bool IsActive { get; set; } = true;
    }
}
