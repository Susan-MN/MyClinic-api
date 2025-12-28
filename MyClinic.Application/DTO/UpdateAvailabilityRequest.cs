using MyClinic.Domain.Entities;

namespace MyClinic.Application.DTO
{
    public class UpdateAvailabilityRequest
    {

        public List<string> WorkingDays { get; set; } = new();
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int SlotDuration { get; set; }
        public bool AcceptBookings { get; set; }



    }
}

