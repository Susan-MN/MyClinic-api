namespace MyClinic.Domain.Entities
{
    public class AvailabilityException
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;
        
        public DateOnly ExceptionDate { get; set; }
        public bool IsAvailable { get; set; } = false; // false = on leave/unavailable
        public TimeSpan? CustomStartTime { get; set; } // optional override hours
        public TimeSpan? CustomEndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ExceptionType Type { get; set; } = ExceptionType.Leave;
    }

 
}
