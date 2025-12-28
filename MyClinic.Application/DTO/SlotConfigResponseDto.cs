namespace MyClinic.Application.DTO
{
    public class SlotConfigResponseDto
    {
        public string Id { get; set; } = "slot-config-id";
        public List<int> AllowedDurations { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

