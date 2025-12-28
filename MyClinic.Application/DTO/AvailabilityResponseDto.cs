namespace MyClinic.Application.DTO
{
    public class AvailabilityResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public List<string> WorkingDays { get; set; } = new();
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int SlotDuration { get; set; }
        public bool IsActive { get; set; }
    }
}

