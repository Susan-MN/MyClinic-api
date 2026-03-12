using MyClinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface ILeaveRepository : IGenericRepository<Leave>
    {
        Task<IEnumerable<Leave>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<Leave>> GetApprovedLeavesByDoctorIdAsync(int doctorId);
        Task<IEnumerable<Leave>> GetLeavesByDateRangeAsync(int doctorId, DateOnly startDate, DateOnly endDate);
        Task<IEnumerable<Leave>> GetAllLeavesAsync();
        Task<IEnumerable<Leave>> GetPendingLeavesAsync();
    }
}
