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
        public string Specialty { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? KeycloakId { get; private set; }

        public void SetKeycloakId(string keycloakId)
        {
            if (string.IsNullOrEmpty(KeycloakId))
                KeycloakId = keycloakId;
        }
    }
}
