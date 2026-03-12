using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface IAvailabilityExceptionRepository : IGenericRepository<AvailabilityException>
    {
        Task<AvailabilityException?> GetByDoctorIdAndDateAsync(int doctorId, DateOnly date);
        Task<IEnumerable<AvailabilityException>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<AvailabilityException>> GetByDoctorIdAndDateRangeAsync(int doctorId, DateOnly startDate, DateOnly endDate);
        Task<IEnumerable<AvailabilityException>> GetApprovedLeavesByDoctorIdAsync(int doctorId);
        Task<IEnumerable<AvailabilityException>> GetPendingLeavesAsync();
        Task<IEnumerable<AvailabilityException>> GetAllLeavesAsync();
    }
}





