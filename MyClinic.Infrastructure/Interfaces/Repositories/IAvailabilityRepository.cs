using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface IAvailabilityRepository : IGenericRepository<Availability>
    {
        Task<Availability?> GetByDoctorIdAsync(int doctorId);
        
    }
}


