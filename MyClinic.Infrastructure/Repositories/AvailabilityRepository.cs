using Microsoft.EntityFrameworkCore;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Interfaces.Repositories;

namespace MyClinic.Infrastructure.Repositories
{
    public class AvailabilityRepository : GenericRepository<Availability>, IAvailabilityRepository
    {
        private readonly AppDbContext _db;

        public AvailabilityRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<Availability?> GetByDoctorIdAsync(int doctorId)
        {
            return await _db.Availabilities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.DoctorId == doctorId);
        }
    }
}


