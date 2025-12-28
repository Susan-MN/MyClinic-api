namespace MyClinic.Application.DTO
{
    public class SlotDto
    {
        public string StartTime { get; set; } = string.Empty; 
        public string EndTime { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}

