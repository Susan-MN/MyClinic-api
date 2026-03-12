using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface IAvailabilityDayRepository : IGenericRepository<AvailabilityDay>
    {
        Task<AvailabilityDay?> GetByDoctorIdAndDayAsync(int doctorId, int dayOfWeek);
        Task<IEnumerable<AvailabilityDay>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<AvailabilityDay>> GetActiveByDoctorIdAsync(int doctorId);
    }
}





