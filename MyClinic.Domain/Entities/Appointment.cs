using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MyClinic.Domain.Entities
{
    public class Appointment
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Foreign Key to User (Patient)
        public User Patient { get; set; } = null!;
        public string PatientName { get; set; } = string.Empty; // Kept as snapshot
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;
        public string? SlotId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending; 
    }
}

