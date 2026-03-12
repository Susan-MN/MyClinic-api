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
    public class AvailabilityExceptionRepository : GenericRepository<AvailabilityException>, IAvailabilityExceptionRepository
    {
        private readonly AppDbContext _db;

        public AvailabilityExceptionRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<AvailabilityException?> GetByDoctorIdAndDateAsync(int doctorId, DateOnly date)
        {
            return await _db.AvailabilityExceptions
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.DoctorId == doctorId && e.ExceptionDate == date);
        }

        public async Task<IEnumerable<AvailabilityException>> GetByDoctorIdAsync(int doctorId)
        {
            return await _db.AvailabilityExceptions
                .AsNoTracking()
                .Where(e => e.DoctorId == doctorId)
                .OrderByDescending(e => e.ExceptionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AvailabilityException>> GetByDoctorIdAndDateRangeAsync(int doctorId, DateOnly startDate, DateOnly endDate)
        {
            return await _db.AvailabilityExceptions
                .AsNoTracking()
                .Where(e => e.DoctorId == doctorId 
                    && e.ExceptionDate >= startDate 
                    && e.ExceptionDate <= endDate
                    && e.IsAvailable == false) // Only return leaves/unavailable exceptions
                .OrderBy(e => e.ExceptionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AvailabilityException>> GetApprovedLeavesByDoctorIdAsync(int doctorId)
        {
            return await _db.AvailabilityExceptions
                .AsNoTracking()
                .Where(e => e.DoctorId == doctorId 
                    && e.Type == ExceptionType.Leave 
                    && e.IsAvailable == false) // Approved leaves are those marked as unavailable
                .OrderByDescending(e => e.ExceptionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AvailabilityException>> GetPendingLeavesAsync()
        {
            // For now, we'll consider all leaves as approved since we're using IsAvailable flag
            // You can add an IsApproved field later if needed
            return await _db.AvailabilityExceptions
                .AsNoTracking()
                .Where(e => e.Type == ExceptionType.Leave)
                .OrderByDescending(e => e.ExceptionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AvailabilityException>> GetAllLeavesAsync()
        {
            return await _db.AvailabilityExceptions
                .AsNoTracking()
                .Where(e => e.Type == ExceptionType.Leave)
                .OrderByDescending(e => e.ExceptionDate)
                .ToListAsync();
        }
    }
}





