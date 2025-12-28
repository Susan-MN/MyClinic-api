using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Domain.Entities
{
    public class Doctor
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string? Specialty { get; set; } 
        public string Email { get; set; } = null!;

        public string? ImageUrl { get; set; }
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
        public string? KeycloakId  { get; private set; }

        public DoctorStatus Status { get; set; } = DoctorStatus.Pending;

        public bool ProfileComplete { get; set; } = false;
        public void SetKeycloakId(string keycloakId)
        {
            if (string.IsNullOrEmpty(KeycloakId))
                KeycloakId = keycloakId;
        }

        public void Approve()
        {
            Status= DoctorStatus.Approved;
        }
        public void Decline()
        {
            Status= DoctorStatus.Declined;
        }
    }
}
