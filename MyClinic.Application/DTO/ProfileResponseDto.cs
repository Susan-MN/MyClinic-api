namespace MyClinic.Application.DTO
{
    public class ProfileResponseDto
    {
        public string Role { get; set; } = string.Empty;
        public string? Status { get; set; }
        public bool ProfileComplete { get; set; }
    }
}

