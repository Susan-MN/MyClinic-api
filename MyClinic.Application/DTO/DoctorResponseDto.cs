using MyClinic.Domain.Entities;

namespace MyClinic.Application.DTO
{
    public class DoctorResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string? Specialty { get; set; } 
        public string Email { get; set; } = null!;
        public string? KeycloakId { get; set; }
        public DoctorStatus Status { get; set; }

        public bool ProfileComplete { get; set; } = false;
        public string? ImageUrl { get; set; }
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
    }
}

