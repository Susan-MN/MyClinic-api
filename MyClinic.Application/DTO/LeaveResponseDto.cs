using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Application.DTO
{
    public class LeaveResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
