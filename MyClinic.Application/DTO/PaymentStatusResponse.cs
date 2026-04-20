using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Application.DTO
{
    public class PaymentStatusResponse
    {
        public int AppointmentId { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
