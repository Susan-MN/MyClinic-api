using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Interfaces.Repositories;

namespace MyClinic.Infrastructure.Repositories
{
    public class AvailabilityDayRepository : GenericRepository<AvailabilityDay>, IAvailabilityDayRepository
    {
        private readonly AppDbContext _db;

        public AvailabilityDayRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<AvailabilityDay?> GetByDoctorIdAndDayAsync(int doctorId, int dayOfWeek)
        {
            return await _db.AvailabilityDays
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.DoctorId == doctorId && a.DayOfWeek == dayOfWeek);
        }

        public async Task<IEnumerable<AvailabilityDay>> GetByDoctorIdAsync(int doctorId)
        {
            return await _db.AvailabilityDays
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId)
                .OrderBy(a => a.DayOfWeek)
                .ToListAsync();
        }

        public async Task<IEnumerable<AvailabilityDay>> GetActiveByDoctorIdAsync(int doctorId)
        {
            return await _db.AvailabilityDays
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId && a.IsActive)
                .OrderBy(a => a.DayOfWeek)
                .ToListAsync();
        }
    }
}





