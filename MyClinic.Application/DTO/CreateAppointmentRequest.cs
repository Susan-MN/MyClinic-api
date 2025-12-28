namespace MyClinic.Application.DTO
{
    public class CreateAppointmentRequest
    {
        public int DoctorId { get; set; }
        public string? SlotId { get; set; } = string.Empty;
        public string AppointmentDate { get; set; } = string.Empty; 
        public string StartTime { get; set; } = string.Empty;       
        public string EndTime { get; set; } = string.Empty;
        
    }
}

