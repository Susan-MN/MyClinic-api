using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Application.DTO
{
     public class AvailabilityDaysRequest
    {
        public List<AvailabilityDayRequest> Days { get; set; } = new();
    }
}
