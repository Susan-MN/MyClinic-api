namespace MyClinic.Application.DTO
{
    public class AppointmentResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string? SlotId { get; set; }
        public string AppointmentDate { get; set; } = string.Empty; // Format: "2025-02-01"
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

