using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MyClinic.Domain.Entities
{
    public class SlotConfig
    {
        public int Id { get; set; }
        public string AllowedDurationsJson { get; set; } = "[]"; // JSON array stored as string
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

