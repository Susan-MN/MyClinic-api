using MyClinic.Domain.Entities;

namespace MyClinic.Application.DTO
{
    public class UpdateDoctorRequest
    {

        public string? Specialty { get; set; }
        public string? Email { get; set; }
        public string? ImageUrl { get; set; }
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }



    }
}

